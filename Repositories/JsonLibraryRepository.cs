using System.Text.Json;
using PersonalLibrary.Interfaces;
using PersonalLibrary.Models;

namespace PersonalLibrary.Repositories
{
    public class JsonLibraryRepository : ILibraryRepository
    {
        private readonly string _libraryFilePath;
        private readonly object _saveLock = new();
        private LibraryData _data;

        public string? LoadError { get; }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonLibraryRepository(string dataDirectory)
        {
            _libraryFilePath = Path.Combine(dataDirectory, "library.json");

            Directory.CreateDirectory(dataDirectory);

            try
            {
                _data = JsonFileStore.Load<LibraryData>(_libraryFilePath, _jsonOptions);
            }
            catch (StorageException ex)
            {
                _data = new LibraryData();
                LoadError = ex.Message;
            }
        }


        public List<Author> GetAllAuthors()
        {
            return _data.Authors.OrderBy(a => a.Name).ToList();
        }

        public Author? GetAuthorById(string id)
        {
            return _data.Authors.FirstOrDefault(a => a.Id == id);
        }

        public void AddAuthor(Author author)
        {
            _data.Authors.Add(author);
            Save();
        }

        public void UpdateAuthor(Author author)
        {
            var existing = GetAuthorById(author.Id);
            if (existing == null) return;

            existing.Name = author.Name;
            existing.PhotoPath = author.PhotoPath;
            existing.IsFavorite = author.IsFavorite;
            Save();
        }

        public void DeleteAuthor(string authorId)
        {
            var author = GetAuthorById(authorId);
            if (author != null)
            {
                _data.Authors.Remove(author);
                Save();
            }
        }


        public Book? GetBookById(string id)
        {
            return _data.Authors
                .SelectMany(a => a.Books.Concat(a.Series.SelectMany(s => s.Books)))
                .FirstOrDefault(b => b.Id == id);
        }

        public void AddBook(string authorId, Book book)
        {
            var author = GetAuthorById(authorId);
            if (author == null) return;

            book.AuthorId = authorId;
            author.Books.Add(book);
            Save();
        }

        public void UpdateBook(Book book)
        {
            var author = GetAuthorById(book.AuthorId);
            if (author == null) return;

            var existing = author.Books.FirstOrDefault(b => b.Id == book.Id);
            if (existing != null)
            {
                existing.Title = book.Title;
                existing.CoverPath = book.CoverPath;
                existing.Flags = book.Flags;
                existing.Note = book.Note;
                existing.IsFavorite = book.IsFavorite;
                existing.IsMissing = book.IsMissing;
                Save();
                return;
            }

            foreach (var series in author.Series)
            {
                var bookInSeries = series.Books.FirstOrDefault(b => b.Id == book.Id);
                if (bookInSeries != null)
                {
                    bookInSeries.Title = book.Title;
                    bookInSeries.CoverPath = book.CoverPath;
                    bookInSeries.Flags = book.Flags;
                    bookInSeries.Note = book.Note;
                    bookInSeries.IsFavorite = book.IsFavorite;
                    bookInSeries.IsMissing = book.IsMissing;
                    Save();
                    return;
                }
            }
        }

        public void DeleteBook(string authorId, string bookId)
        {
            var author = GetAuthorById(authorId);
            if (author == null) return;

            var book = author.Books.FirstOrDefault(b => b.Id == bookId);
            if (book != null)
            {
                author.Books.Remove(book);
                Save();
                return;
            }

            foreach (var series in author.Series)
            {
                var bookInSeries = series.Books.FirstOrDefault(b => b.Id == bookId);
                if (bookInSeries != null)
                {
                    series.Books.Remove(bookInSeries);
                    ReindexSeriesBooks(series);
                    Save();
                    return;
                }
            }
        }


        public Series? GetSeriesById(string id)
        {
            return _data.Authors
                .SelectMany(a => a.Series)
                .FirstOrDefault(s => s.Id == id);
        }

        public void AddSeries(string authorId, Series series)
        {
            var author = GetAuthorById(authorId);
            if (author == null) return;

            series.AuthorId = authorId;
            author.Series.Add(series);
            Save();
        }

        public void UpdateSeries(Series series)
        {
            var existing = GetSeriesById(series.Id);
            if (existing == null) return;

            existing.Name = series.Name;
            existing.CoverPath = series.CoverPath;
            existing.IsOngoing = series.IsOngoing;
            existing.IsFavorite = series.IsFavorite;
            Save();
        }

        public void DeleteSeries(string authorId, string seriesId)
        {
            var author = GetAuthorById(authorId);
            if (author == null) return;

            var series = author.Series.FirstOrDefault(s => s.Id == seriesId);
            if (series != null)
            {
                author.Series.Remove(series);
                Save();
            }
        }


        public void AddBookToSeries(string seriesId, Book book)
        {
            var series = GetSeriesById(seriesId);
            if (series == null) return;

            series.AddBookToSeries(book);
            Save();
        }

        public void Save()
        {
            if (LoadError != null)
                throw new StorageException("library.json was not loaded; saving is disabled to protect the data.");
            lock (_saveLock)
                JsonFileStore.Save(_libraryFilePath, _data, _jsonOptions);
        }

        private static void ReindexSeriesBooks(Series series)
        {
            var ordered = series.Books.OrderBy(b => b.OrderInSeries ?? int.MaxValue).ToList();
            for (int i = 0; i < ordered.Count; i++)
                ordered[i].OrderInSeries = i + 1;
        }
    }
}
