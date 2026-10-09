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

        public LibraryService(ILibraryRepository repository, LibraryEventPublisher publisher)
        {
            _repository = repository;
            _publisher = publisher;
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
                return (false, $"Author \"{author.Name}\" already exists in the catalogue.");

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

            author.PhotoPath = photoPath;
            _repository.UpdateAuthor(author);
            _publisher.AuthorPhotoUpdated(author.Name);
        }


        public Book? GetBookById(string id) => _repository.GetBookById(id);

        public (bool Success, string Message) AddBook(string authorId, Book book)
        {
            var author = _repository.GetAuthorById(authorId);
            if (author == null) return (false, "Author not found.");

            var titleTrimmed = book.Title.Trim();

            bool alreadyExists = author.Books.Any(b => b.Title.Equals(titleTrimmed, StringComparison.OrdinalIgnoreCase))
                || author.Series.Any(s => s.Books.Any(b => b.Title.Equals(titleTrimmed, StringComparison.OrdinalIgnoreCase)));

            if (alreadyExists)
                return (false, $"Book \"{titleTrimmed}\" already exists for this author.");

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
            if (book == null) return (false, "Book not found.");

            var author = _repository.GetAuthorById(book.AuthorId);
            if (author == null) return (false, "Author not found.");

            var newTitleTrimmed = newTitle.Trim();

            if (!book.IsMissing)
            {
                var allTitles = author.Books
                    .Where(b => b.Id != bookId)
                    .Select(b => b.Title)
                    .Concat(author.Series.SelectMany(s => s.Books.Where(b => b.Id != bookId).Select(b => b.Title)));

                if (allTitles.Any(t => t.Equals(newTitleTrimmed, StringComparison.OrdinalIgnoreCase)))
                    return (false, $"Book \"{newTitleTrimmed}\" already exists for this author.");
            }

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
            if (book == null) return (false, "Book not found.");

            if (isMissing.HasValue && !string.IsNullOrEmpty(book.SeriesId))
            {
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

            book.CoverPath = coverPath;
            _repository.UpdateBook(book);
            var _coverAuthor = _repository.GetAuthorById(book.AuthorId);
            if (_coverAuthor != null) _publisher.BookCoverUpdated(book.Title, _coverAuthor.Name);
        }


        public Series? GetSeriesById(string id) => _repository.GetSeriesById(id);

        public (bool Success, string Message) AddSeries(string authorId, Series series)
        {
            var author = _repository.GetAuthorById(authorId);
            if (author == null) return (false, "Author not found.");

            var duplicate = author.Series.FirstOrDefault(s =>
                s.Name.Equals(series.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
                return (false, $"Series \"{series.Name}\" already exists for this author.");

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

        public (bool Success, string Message) RenameSeries(string seriesId, string newName)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Series not found.");

            var author = _repository.GetAuthorById(series.AuthorId);
            if (author == null) return (false, "Author not found.");

            var newNameTrimmed = newName.Trim();

            var duplicate = author.Series.FirstOrDefault(s =>
                s.Id != seriesId &&
                s.Name.Equals(newNameTrimmed, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
                return (false, $"Series \"{newNameTrimmed}\" already exists for this author.");

            string oldName = series.Name;
            series.Name = newNameTrimmed;
            _repository.UpdateSeries(series);
            _publisher.SeriesRenamed(oldName, newNameTrimmed, author.Name);

            return (true, string.Empty);
        }


        public (bool Success, string Message) SetSeriesOngoing(string seriesId, bool isOngoing)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Series not found.");

            series.IsOngoing = isOngoing;
            _repository.UpdateSeries(series);
            return (true, string.Empty);
        }

        public void UpdateSeriesCover(string seriesId, string coverPath)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return;
            series.CoverPath = coverPath;
            _repository.UpdateSeries(series);
            var author = _repository.GetAuthorById(series.AuthorId);
            if (author != null) _publisher.SeriesCoverUpdated(series.Name, author.Name);
        }

        public (bool Success, string Message) AddBookToSeries(string seriesId, Book book)
        {
            var series = _repository.GetSeriesById(seriesId);
            if (series == null) return (false, "Series not found.");

            var author = _repository.GetAuthorById(series.AuthorId);
            if (author == null) return (false, "Author not found.");

            var titleTrimmed = string.IsNullOrWhiteSpace(book.Title) ? "-" : book.Title.Trim();
            if (!book.IsMissing)
            {
                var duplicate = series.Books.FirstOrDefault(b =>
                    b.Title.Equals(titleTrimmed, StringComparison.OrdinalIgnoreCase));
                if (duplicate != null)
                    return (false, $"Book \"{titleTrimmed}\" already exists in this series.");
            }

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
                    if (author == null) return (false, "Author not found.");
                    author.IsFavorite = isFavorite;
                    _repository.UpdateAuthor(author);
                    return (true, string.Empty);
                case "series":
                    var series = _repository.GetSeriesById(id);
                    if (series == null) return (false, "Series not found.");
                    series.IsFavorite = isFavorite;
                    _repository.UpdateSeries(series);
                    return (true, string.Empty);
                case "book":
                    var book = _repository.GetBookById(id);
                    if (book == null) return (false, "Book not found.");
                    if (book.IsMissing && isFavorite) return (false, "A missing book cannot be a favourite.");
                    book.IsFavorite = isFavorite && !book.IsMissing;
                    _repository.UpdateBook(book);
                    return (true, string.Empty);
                default:
                    return (false, "Unknown item type.");
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
                    PhotoPath   = a.PhotoPath,
                    BookCount   = a.Books.Count(b => !b.IsMissing) + a.Series.Sum(s => s.Books.Count(b => !b.IsMissing)),
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
                    BookCount  = s.Books.Count(b => !b.IsMissing),
                    IsOngoing  = s.IsOngoing
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
                    BookCount  = s.Books.Count(b => !b.IsMissing),
                    IsOngoing  = true
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
        public bool IsOngoing { get; set; }
    }

    public class FavoriteAuthorInfo
    {
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
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
        public bool IsFavorite { get; set; }

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
            Note          = book.Note,
            IsFavorite    = book.IsFavoriteActive
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
