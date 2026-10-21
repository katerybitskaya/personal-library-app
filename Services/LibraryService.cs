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

        public const string OtherLetter = "#";

        private static string LetterOf(string name)
        {
            var first = name.Trim().FirstOrDefault();
            return char.IsLetter(first) ? char.ToUpperInvariant(first).ToString() : OtherLetter;
        }

        public List<(string Letter, List<Author> Authors)> GetAuthorsByLetter(string alphabet)
        {
            int Rank(string letter)
            {
                if (letter == OtherLetter) return int.MaxValue;
                int i = alphabet.IndexOf(letter, StringComparison.Ordinal);
                return i >= 0 ? i : alphabet.Length;
            }

            return _repository.GetAllAuthors()
                .GroupBy(a => LetterOf(a.Name))
                .OrderBy(g => Rank(g.Key))
                .ThenBy(g => g.Key, StringComparer.InvariantCulture)
                .Select(g => (g.Key, g.ToList()))
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

        private const int NoteMaxLength = 1000;

        private static string? CleanNote(string? note)
        {
            var trimmed = note?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return null;
            return trimmed.Length > NoteMaxLength ? trimmed[..NoteMaxLength] : trimmed;
        }

        public (bool Success, string Message) SetBookSeriesNumber(string bookId, string? number)
        {
            var book = _repository.GetBookById(bookId);
            if (book == null || string.IsNullOrEmpty(book.SeriesId)) return (false, "Error_NotFound");
            var trimmed = number?.Trim();
            if (trimmed?.Length > SeriesNumberMaxLength) return (false, "Error_Validation");
            book.SeriesNumber = string.IsNullOrEmpty(trimmed) ? null : trimmed;
            _repository.UpdateBook(book);
            return (true, string.Empty);
        }

        private const int SeriesNumberMaxLength = 10;

        public (bool Success, string Message) UpdateAuthorNote(string authorId, string? note)
        {
            var author = _repository.GetAuthorById(authorId);
            if (author == null) return (false, "Error_NotFound");
            author.Note = CleanNote(note);
            _repository.UpdateAuthor(author);
            return (true, string.Empty);
        }

        public (bool Success, string Message) UpdateSeriesNote(string seriesId, string? note)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Error_NotFound");
            series.Note = CleanNote(note);
            _repository.UpdateSeries(series);
            return (true, string.Empty);
        }

        public (bool Success, string Message) RemoveAuthorPhoto(string authorId)
        {
            var author = _repository.GetAuthorById(authorId);
            if (author == null) return (false, "Error_NotFound");
            if (string.IsNullOrEmpty(author.PhotoPath)) return (true, string.Empty);

            var oldPath = author.PhotoPath;
            author.PhotoPath = null;
            _repository.UpdateAuthor(author);
            _uploads.DeleteIfUnused(new[] { oldPath });
            _publisher.AuthorPhotoRemoved(author.Name);
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
                if (book.IsMissing) book.FavoriteBy.Clear();
            }

            var allowed = BookFlags.AllowedFor(book);
            book.Flags = (flags ?? Enumerable.Empty<BookFlag>())
                .Where(f => allowed.Contains(f))
                .Distinct()
                .OrderBy(f => f)
                .ToList();
            if (BookFlags.Priority.All(book.Flags.Contains))
                book.Flags.Remove(BookFlag.LowPriority);

            book.Note = CleanNote(note);

            _repository.UpdateBook(book);
            return (true, string.Empty);
        }

        public (bool Success, string Message) RemoveBookCover(string bookId)
        {
            var book = _repository.GetBookById(bookId);
            if (book == null) return (false, "Error_NotFound");
            if (string.IsNullOrEmpty(book.CoverPath)) return (true, string.Empty);

            var oldPath = book.CoverPath;
            book.CoverPath = null;
            _repository.UpdateBook(book);
            _uploads.DeleteIfUnused(new[] { oldPath });
            _publisher.BookCoverRemoved(book.Title, _repository.GetAuthorById(book.AuthorId)?.Name ?? "");
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
            if (newNameTrimmed.Length == 0 || newNameTrimmed.Length > 200) return (false, "Error_Validation");
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

        public (bool Success, string Message) RemoveSeriesCover(string seriesId)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Error_NotFound");
            if (string.IsNullOrEmpty(series.CoverPath)) return (true, string.Empty);

            var oldPath = series.CoverPath;
            series.CoverPath = null;
            _repository.UpdateSeries(series);
            _uploads.DeleteIfUnused(new[] { oldPath });
            _publisher.SeriesCoverRemoved(series.Name, _repository.GetAuthorById(series.AuthorId)?.Name ?? "");
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
                            Number = book.DisplayNumber,
                            Title = book.Title,
                            Flags = book.ActiveFlags,
                            Note = book.Note
                        });
                    }
                }
            }

            return result;
        }

        public (bool Success, string Message) SetFavorite(string kind, string id, bool isFavorite, string userId)
        {
            switch (kind)
            {
                case "author":
                    var author = _repository.GetAuthorById(id);
                    if (author == null) return (false, "Error_NotFound");
                    Favorites.Set(author.FavoriteBy, userId, isFavorite);
                    _repository.UpdateAuthor(author);
                    return (true, string.Empty);
                case "series":
                    var series = _repository.GetSeriesById(id);
                    if (series == null) return (false, "Error_NotFound");
                    Favorites.Set(series.FavoriteBy, userId, isFavorite);
                    _repository.UpdateSeries(series);
                    return (true, string.Empty);
                case "book":
                    var book = _repository.GetBookById(id);
                    if (book == null) return (false, "Error_NotFound");
                    if (book.IsMissing && isFavorite) return (false, "Error_MissingNotFavorite");
                    Favorites.Set(book.FavoriteBy, userId, isFavorite);
                    _repository.UpdateBook(book);
                    return (true, string.Empty);
                default:
                    return (false, "Error_Unknown");
            }
        }

        private static bool IsFavoriteFor(List<string> favoriteBy, string? userId) =>
            userId == null ? favoriteBy.Count > 0 : favoriteBy.Contains(userId);

        public void RemoveFavoritesOf(string userId)
        {
            foreach (var author in _repository.GetAllAuthors())
            {
                author.FavoriteBy.Remove(userId);
                foreach (var book in author.Books) book.FavoriteBy.Remove(userId);
                foreach (var series in author.Series)
                {
                    series.FavoriteBy.Remove(userId);
                    foreach (var book in series.Books) book.FavoriteBy.Remove(userId);
                }
            }
            _repository.Save();
        }

        public Dictionary<string, int> CountFavoritesByUser()
        {
            var ids = _repository.GetAllAuthors().SelectMany(a => a.FavoriteBy
                .Concat(a.Series.SelectMany(s => s.FavoriteBy))
                .Concat(a.Books.Concat(a.Series.SelectMany(s => s.Books)).SelectMany(b => b.ActiveFavoriteBy)));
            return ids.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        }

        public List<FavoriteAuthorInfo> GetFavoriteAuthors(string? userId)
        {
            return _repository.GetAllAuthors()
                .Where(a => IsFavoriteFor(a.FavoriteBy, userId))
                .Select(a => new FavoriteAuthorInfo
                {
                    AuthorId    = a.Id,
                    AuthorName  = a.Name,
                    BookCount   = a.Books.Count + a.Series.Sum(s => s.Books.Count),
                    SeriesCount = a.Series.Count,
                    Note        = a.Note,
                    FavoriteBy  = a.FavoriteBy.ToList()
                })
                .ToList();
        }

        public List<OngoingSeriesInfo> GetFavoriteSeries(string? userId)
        {
            return _repository.GetAllAuthors()
                .SelectMany(a => a.Series.Where(s => IsFavoriteFor(s.FavoriteBy, userId)).OrderBy(s => s.Name).Select(s => new OngoingSeriesInfo
                {
                    AuthorId   = a.Id,
                    AuthorName = a.Name,
                    SeriesId   = s.Id,
                    SeriesName = s.Name,
                    BookCount  = s.Books.Count,
                    Note       = s.Note,
                    FavoriteBy = s.FavoriteBy.ToList()
                }))
                .ToList();
        }

        public List<FlaggedBookInfo> GetFavoriteBooks(string? userId)
        {
            var result = new List<FlaggedBookInfo>();
            foreach (var author in _repository.GetAllAuthors())
            {
                foreach (var book in author.Books.Where(b => IsFavoriteFor(b.ActiveFavoriteBy, userId)).OrderBy(b => b.Title))
                    result.Add(FlaggedBookInfo.From(author, null, book));
                foreach (var series in author.Series.OrderBy(s => s.Name))
                    foreach (var book in series.Books.Where(b => IsFavoriteFor(b.ActiveFavoriteBy, userId)).OrderBy(b => b.OrderInSeries ?? int.MaxValue))
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
                    BookCount  = s.Books.Count,
                    Note       = s.Note
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
                foreach (var book in author.Books.Where(b => b.HasFlags && !b.IsMissing).OrderBy(b => b.Title))
                    result.Add(FlaggedBookInfo.From(author, null, book));

                foreach (var series in author.Series.OrderBy(s => s.Name))
                    foreach (var book in series.Books.Where(b => b.HasFlags && !b.IsMissing).OrderBy(b => b.OrderInSeries ?? int.MaxValue))
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
        public string? Note { get; set; }
        public List<string> FavoriteBy { get; set; } = new();
    }

    public class FavoriteAuthorInfo
    {
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public int SeriesCount { get; set; }
        public string? Note { get; set; }
        public List<string> FavoriteBy { get; set; } = new();
    }

    public class FlaggedBookInfo
    {
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? SeriesId { get; set; }
        public string? SeriesName { get; set; }
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Number { get; set; }
        public List<BookFlag> Flags { get; set; } = new();
        public string? Note { get; set; }
        public List<string> FavoriteBy { get; set; } = new();

        public static FlaggedBookInfo From(Author author, Series? series, Book book) => new()
        {
            AuthorId      = author.Id,
            AuthorName    = author.Name,
            SeriesId      = series?.Id,
            SeriesName    = series?.Name,
            BookId        = book.Id,
            Title         = book.Title,
            Number        = series != null ? book.DisplayNumber : null,
            Flags         = book.ActiveFlags,
            Note          = book.Note,
            FavoriteBy    = book.ActiveFavoriteBy.ToList()
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
        public string Number { get; set; } = string.Empty;
        public List<BookFlag> Flags { get; set; } = new();
        public string? Note { get; set; }
    }
}
