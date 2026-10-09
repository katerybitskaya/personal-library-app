using PersonalLibrary.Interfaces;
using PersonalLibrary.Models;
using PersonalLibrary.Patterns;
using PersonalLibrary.Repositories;

namespace PersonalLibrary.Services
{
    public class LibraryService
    {
        private readonly ILibraryRepository _repository;
        private readonly LibraryEventPublisher _publisher;
        private readonly UploadCleanupService _uploads;

        public LibraryService(ILibraryRepository repository, LibraryEventPublisher publisher, UploadCleanupService uploads)
        {
            _repository = repository;
            _publisher = publisher;
            _uploads = uploads;
        }

        private LibrarySearchIterator FreshIterator() =>
            new LibrarySearchIterator(_repository.GetAllAuthors());


        public List<Author> GetAllAuthors() => _repository.GetAllAuthors();

        public Author? GetAuthorById(string id) => _repository.GetAuthorById(id);

        public Dictionary<char, List<Author>> GetAuthorsByLetter()
        {
            return _repository.GetAllAuthors()
                .GroupBy(a => char.ToUpper(a.Name.FirstOrDefault()))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public List<char> GetExistingLetters()
        {
            return _repository.GetAllAuthors()
                .Select(a => char.ToUpper(a.Name.FirstOrDefault()))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        public (bool Success, string Message) AddAuthor(Author author)
        {
            var existing = _repository.GetAllAuthors()
                .FirstOrDefault(a => a.Name.Equals(author.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                return (false, "Author_AlreadyExists");

            author.Name = author.Name.Trim();
            author.Id = Guid.NewGuid().ToString();
            author.DateAdded = DateTime.Now;

            _repository.AddAuthor(author);

            return (true, string.Empty);
        }

        public void UpdateAuthorPhoto(string authorId, string photoPath)
        {
            var author = _repository.GetAuthorById(authorId);
            if (author == null) return;

            var oldPath = author.PhotoPath;
            author.PhotoPath = photoPath;
            _repository.UpdateAuthor(author);
            _uploads.DeleteIfUnused(new[] { oldPath });
            _publisher.AuthorPhotoUpdated(author.Name);
        }


        public Book? GetBookById(string id) => _repository.GetBookById(id);

        // Название уникально в пределах одного места: отдельные книги автора или одна серия
        public static bool IsTitleTaken(IEnumerable<Book> books, string title, string? exceptBookId = null)
        {
            if (string.IsNullOrWhiteSpace(title)) return false;
            return books.Any(b => b.Id != exceptBookId && b.Title.Trim().Equals(title.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public (bool Success, string Message) AddBook(string authorId, Book book)
        {
            var author = _repository.GetAuthorById(authorId);
            if (author == null) return (false, "Error_NotFound");

            if (IsTitleTaken(author.Books, book.Title))
                return (false, "Book_AlreadyExists");

            book.Title = book.Title.Trim();
            book.NormalizeMissing();
            book.Id = Guid.NewGuid().ToString();
            book.AuthorId = authorId;
            book.DateAdded = DateTime.Now;

            _repository.AddBook(authorId, book);

            return (true, string.Empty);
        }

        public (bool Success, string Message) RenameBook(string bookId, string newTitle)
        {
            var book = _repository.GetBookById(bookId);
            if (book == null) return (false, "Error_NotFound");

            var author = _repository.GetAuthorById(book.AuthorId);
            if (author == null) return (false, "Error_NotFound");

            var newTitleTrimmed = newTitle.Trim();

            var siblings = author.Series.FirstOrDefault(s => s.Id == book.SeriesId)?.Books ?? author.Books;
            if (IsTitleTaken(siblings, newTitleTrimmed, bookId))
                return (false, "Book_AlreadyExists");

            string oldTitle = book.Title;
            book.Title = newTitleTrimmed;
            book.NormalizeMissing();
            _repository.UpdateBook(book);
            var _renameSeries = author.Series.FirstOrDefault(s => s.Books.Any(b => b.Id == bookId));
            _publisher.BookRenamed(oldTitle, newTitleTrimmed, author.Name, _renameSeries?.Name);

            return (true, string.Empty);
        }

        public (bool Success, string Message) UpdateBookFlags(string bookId, IEnumerable<BookFlag>? flags, string? note, bool? isMissing = null)
        {
            var book = _repository.GetBookById(bookId);
            if (book == null) return (false, "Error_NotFound");

            if (isMissing.HasValue && !string.IsNullOrEmpty(book.SeriesId))
            {
                if (!isMissing.Value && string.IsNullOrWhiteSpace(book.Title))
                    return (false, "Book_MissingNeedsTitle");
                book.IsMissing = isMissing.Value;
                if (book.IsMissing) book.IsFavorite = false;
            }

            var allowed = BookFlags.AllowedFor(book);
            book.Flags = (flags ?? Enumerable.Empty<BookFlag>())
                .Where(f => allowed.Contains(f))
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            var noteTrimmed = note?.Trim();
            book.Note = book.Flags.Count > 0 && !string.IsNullOrEmpty(noteTrimmed) ? noteTrimmed : null;

            _repository.UpdateBook(book);
            return (true, string.Empty);
        }

        public void UpdateBookCover(string bookId, string coverPath)
        {
            var book = _repository.GetBookById(bookId);
            if (book == null) return;

            var oldPath = book.CoverPath;
            book.CoverPath = coverPath;
            _repository.UpdateBook(book);
            _uploads.DeleteIfUnused(new[] { oldPath });
            var _coverAuthor = _repository.GetAuthorById(book.AuthorId);
            if (_coverAuthor != null) _publisher.BookCoverUpdated(book.Title, _coverAuthor.Name);
        }


        public Series? GetSeriesById(string id) => _repository.GetSeriesById(id);

        public (bool Success, string Message) AddSeries(string authorId, Series series)
        {
            var author = _repository.GetAuthorById(authorId);
            if (author == null) return (false, "Error_NotFound");

            var duplicate = author.Series.FirstOrDefault(s =>
                s.Name.Equals(series.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
                return (false, "Series_AlreadyExists");

            var partTitles = series.Books.Select(b => b.Title.Trim()).Where(t => t.Length > 0).ToList();
            if (partTitles.GroupBy(t => t, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                return (false, "Book_AlreadyExists");

            series.Name = series.Name.Trim();
            series.Id = Guid.NewGuid().ToString();
            series.AuthorId = authorId;
            series.DateAdded = DateTime.Now;

            foreach (var book in series.Books)
            {
                book.NormalizeMissing();
                book.Id = Guid.NewGuid().ToString();
                book.AuthorId = authorId;
                book.SeriesId = series.Id;
                book.DateAdded = DateTime.Now;
            }

            _repository.AddSeries(authorId, series);

            return (true, string.Empty);
        }

        public (bool Success, string Message) RenameAuthor(string authorId, string newName)
        {
            var author = _repository.GetAuthorById(authorId);
            if (author == null) return (false, "Error_NotFound");

            var newNameTrimmed = newName.Trim();
            if (_repository.GetAllAuthors().Any(a => a.Id != authorId &&
                    a.Name.Trim().Equals(newNameTrimmed, StringComparison.OrdinalIgnoreCase)))
                return (false, "Author_AlreadyExists");

            string oldName = author.Name;
            if (oldName == newNameTrimmed) return (true, string.Empty);
            author.Name = newNameTrimmed;
            _repository.UpdateAuthor(author);
            _publisher.AuthorRenamed(oldName, newNameTrimmed);

            return (true, string.Empty);
        }

        public (bool Success, string Message) RenameSeries(string seriesId, string newName)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Error_NotFound");

            var author = _repository.GetAuthorById(series.AuthorId);
            if (author == null) return (false, "Error_NotFound");

            var newNameTrimmed = newName.Trim();

            var duplicate = author.Series.FirstOrDefault(s =>
                s.Id != seriesId &&
                s.Name.Equals(newNameTrimmed, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
                return (false, "Series_AlreadyExists");

            string oldName = series.Name;
            series.Name = newNameTrimmed;
            _repository.UpdateSeries(series);
            _publisher.SeriesRenamed(oldName, newNameTrimmed, author.Name);

            return (true, string.Empty);
        }


        // Новый порядок книг серии (перетаскивание / стрелки): номера, что уже были в серии,
        // раздаются книгам по новому порядку — пропуски в нумерации сохраняются
        public (bool Success, string Message) ReorderSeriesBooks(string seriesId, IList<string> bookIds)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Error_NotFound");
            if (bookIds.Count != series.Books.Count || bookIds.Distinct().Count() != bookIds.Count
                || bookIds.Any(id => series.Books.All(b => b.Id != id)))
                return (false, "Error_Unknown");

            var numbers = new List<int>();
            foreach (var n in series.Books.Where(b => b.OrderInSeries.HasValue).Select(b => b.OrderInSeries!.Value).OrderBy(n => n))
                numbers.Add(numbers.Count > 0 && n <= numbers[^1] ? numbers[^1] + 1 : Math.Max(n, 1));
            while (numbers.Count < bookIds.Count)
                numbers.Add(numbers.Count > 0 ? numbers[^1] + 1 : 1);

            for (int i = 0; i < bookIds.Count; i++)
                series.Books.First(b => b.Id == bookIds[i]).OrderInSeries = numbers[i];

            _repository.UpdateSeries(series);
            return (true, string.Empty);
        }

        public (bool Success, string Message) SetSeriesOngoing(string seriesId, bool isOngoing)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Error_NotFound");

            series.IsOngoing = isOngoing;
            _repository.UpdateSeries(series);
            return (true, string.Empty);
        }

        public void UpdateSeriesCover(string seriesId, string coverPath)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return;
            var oldPath = series.CoverPath;
            series.CoverPath = coverPath;
            _repository.UpdateSeries(series);
            _uploads.DeleteIfUnused(new[] { oldPath });
            var author = _repository.GetAuthorById(series.AuthorId);
            if (author != null) _publisher.SeriesCoverUpdated(series.Name, author.Name);
        }

        public (bool Success, string Message) AddBookToSeries(string seriesId, Book book)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Error_NotFound");

            var author = _repository.GetAuthorById(series.AuthorId);
            if (author == null) return (false, "Error_NotFound");

            var titleTrimmed = book.Title?.Trim() ?? string.Empty;
            if (IsTitleTaken(series.Books, titleTrimmed))
                return (false, "Book_AlreadyExists");

            book.Title = titleTrimmed;
            book.NormalizeMissing();
            book.Id = Guid.NewGuid().ToString();
            book.AuthorId = series.AuthorId;
            book.SeriesId = seriesId;
            book.DateAdded = DateTime.Now;

            _repository.AddBookToSeries(seriesId, book);

            return (true, string.Empty);
        }


        public SearchResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new SearchResult();

            query = query.Trim();

            var iter = FreshIterator();
            return new SearchResult
            {
                Query = query,
                MatchedAuthors = iter.FindAuthorsByPrefix(query),
                MatchedBooks = iter.FindBooksByPrefix(query),
                MatchedSeries = iter.FindSeriesByPrefix(query)
            };
        }

        public List<MissingBookInfo> GetMissingBooks()
        {
            var result = new List<MissingBookInfo>();

            foreach (var author in _repository.GetAllAuthors())
            {
                foreach (var series in author.Series)
                {
                    var missingBooks = series.Books
                        .Where(b => b.IsMissing)
                        .OrderBy(b => b.OrderInSeries);

                    foreach (var book in missingBooks)
                    {
                        result.Add(new MissingBookInfo
                        {
                            AuthorId = author.Id,
                            AuthorName = author.Name,
                            SeriesId = series.Id,
                            SeriesName = series.Name,
                            BookId = book.Id,
                            OrderInSeries = book.OrderInSeries ?? 0,
                            Title = book.Title,
                            Flags = book.ActiveFlags,
                            Note = book.HasFlags ? book.Note : null
                        });
                    }
                }
            }

            return result;
        }

        public (bool Success, string Message) SetFavorite(string kind, string id, bool isFavorite)
        {
            switch (kind)
            {
                case "author":
                    var author = _repository.GetAuthorById(id);
                    if (author == null) return (false, "Error_NotFound");
                    author.IsFavorite = isFavorite;
                    _repository.UpdateAuthor(author);
                    return (true, string.Empty);
                case "series":
                    var series = _repository.GetSeriesById(id);
                    if (series == null) return (false, "Error_NotFound");
                    series.IsFavorite = isFavorite;
                    _repository.UpdateSeries(series);
                    return (true, string.Empty);
                case "book":
                    var book = _repository.GetBookById(id);
                    if (book == null) return (false, "Error_NotFound");
                    if (book.IsMissing && isFavorite) return (false, "Error_MissingNotFavorite");
                    book.IsFavorite = isFavorite && !book.IsMissing;
                    _repository.UpdateBook(book);
                    return (true, string.Empty);
                default:
                    return (false, "Error_Unknown");
            }
        }

        public List<FavoriteAuthorInfo> GetFavoriteAuthors()
        {
            return _repository.GetAllAuthors()
                .Where(a => a.IsFavorite)
                .Select(a => new FavoriteAuthorInfo
                {
                    AuthorId    = a.Id,
                    AuthorName  = a.Name,
                    BookCount   = a.Books.Count + a.Series.Sum(s => s.Books.Count),
                    SeriesCount = a.Series.Count
                })
                .ToList();
        }

        public List<OngoingSeriesInfo> GetFavoriteSeries()
        {
            return _repository.GetAllAuthors()
                .SelectMany(a => a.Series.Where(s => s.IsFavorite).OrderBy(s => s.Name).Select(s => new OngoingSeriesInfo
                {
                    AuthorId   = a.Id,
                    AuthorName = a.Name,
                    SeriesId   = s.Id,
                    SeriesName = s.Name,
                    BookCount  = s.Books.Count
                }))
                .ToList();
        }

        public List<FlaggedBookInfo> GetFavoriteBooks()
        {
            var result = new List<FlaggedBookInfo>();
            foreach (var author in _repository.GetAllAuthors())
            {
                foreach (var book in author.Books.Where(b => b.IsFavoriteActive).OrderBy(b => b.Title))
                    result.Add(FlaggedBookInfo.From(author, null, book));
                foreach (var series in author.Series.OrderBy(s => s.Name))
                    foreach (var book in series.Books.Where(b => b.IsFavoriteActive).OrderBy(b => b.OrderInSeries ?? int.MaxValue))
                        result.Add(FlaggedBookInfo.From(author, series, book));
            }
            return result;
        }

        public List<OngoingSeriesInfo> GetOngoingSeries()
        {
            return _repository.GetAllAuthors()
                .SelectMany(a => a.Series.Where(s => s.IsOngoing).Select(s => new OngoingSeriesInfo
                {
                    AuthorId   = a.Id,
                    AuthorName = a.Name,
                    SeriesId   = s.Id,
                    SeriesName = s.Name,
                    BookCount  = s.Books.Count
                }))
                .OrderBy(i => i.AuthorName)
                .ThenBy(i => i.SeriesName)
                .ToList();
        }

        public List<FlaggedBookInfo> GetFlaggedBooks()
        {
            var result = new List<FlaggedBookInfo>();

            foreach (var author in _repository.GetAllAuthors())
            {
                foreach (var book in author.Books.Where(b => b.HasFlags).OrderBy(b => b.Title))
                    result.Add(FlaggedBookInfo.From(author, null, book));

                foreach (var series in author.Series.OrderBy(s => s.Name))
                    foreach (var book in series.Books.Where(b => b.HasFlags).OrderBy(b => b.OrderInSeries ?? int.MaxValue))
                        result.Add(FlaggedBookInfo.From(author, series, book));
            }

            return result;
        }
    }

    public class OngoingSeriesInfo
    {
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string SeriesId { get; set; } = string.Empty;
        public string SeriesName { get; set; } = string.Empty;
        public int BookCount { get; set; }
    }

    public class FavoriteAuthorInfo
    {
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public int SeriesCount { get; set; }
    }

    public class FlaggedBookInfo
    {
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? SeriesId { get; set; }
        public string? SeriesName { get; set; }
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? OrderInSeries { get; set; }
        public bool IsMissing { get; set; }
        public List<BookFlag> Flags { get; set; } = new();
        public string? Note { get; set; }

        public static FlaggedBookInfo From(Author author, Series? series, Book book) => new()
        {
            AuthorId      = author.Id,
            AuthorName    = author.Name,
            SeriesId      = series?.Id,
            SeriesName    = series?.Name,
            BookId        = book.Id,
            Title         = book.Title,
            OrderInSeries = series != null ? book.OrderInSeries : null,
            IsMissing     = book.IsMissing,
            Flags         = book.ActiveFlags,
            Note          = book.Note
        };
    }

    public class SearchResult
    {
        public string Query { get; set; } = string.Empty;
        public List<Author> MatchedAuthors { get; set; } = new();
        public List<(Author Author, Book Book, Series? Series)> MatchedBooks { get; set; } = new();
        public List<(Author Author, Series Series)> MatchedSeries { get; set; } = new();

        public bool IsEmpty => !MatchedAuthors.Any() && !MatchedBooks.Any() && !MatchedSeries.Any();
    }

    public class MissingBookInfo
    {
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string SeriesId { get; set; } = string.Empty;
        public string SeriesName { get; set; } = string.Empty;
        public string BookId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public int OrderInSeries { get; set; }
        public List<BookFlag> Flags { get; set; } = new();
        public string? Note { get; set; }
    }
}
