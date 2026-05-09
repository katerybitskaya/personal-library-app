using PersonalLibrary.Models;

namespace PersonalLibrary.Patterns
{
    public interface ILibraryIterator<T>
    {
        bool HasNext();
        T Next();
        void Reset();
    }

    public interface IIterableCollection<T>
    {
        ILibraryIterator<T> CreateIterator();
    }

    public class AuthorIterator : ILibraryIterator<Author>
    {
        private readonly List<Author> _authors;
        private int _position = 0;

        public AuthorIterator(List<Author> authors) => _authors = authors;

        public bool HasNext() => _position < _authors.Count;

        public Author Next()
        {
            if (!HasNext()) throw new InvalidOperationException("No more authors.");
            return _authors[_position++];
        }

        public void Reset() => _position = 0;
    }

    public class BookIterator : ILibraryIterator<Book>
    {
        private readonly List<Book> _allBooks;
        private int _position = 0;

        public BookIterator(List<Author> authors)
        {
            _allBooks = authors
                .SelectMany(a =>
                    a.Books.Concat(a.Series.SelectMany(s => s.Books)))
                .ToList();
        }

        public bool HasNext() => _position < _allBooks.Count;

        public Book Next()
        {
            if (!HasNext()) throw new InvalidOperationException("No more books.");
            return _allBooks[_position++];
        }

        public void Reset() => _position = 0;
    }

    public class SeriesIterator : ILibraryIterator<Series>
    {
        private readonly List<Series> _allSeries;
        private int _position = 0;

        public SeriesIterator(List<Author> authors)
        {
            _allSeries = authors.SelectMany(a => a.Series).ToList();
        }

        public bool HasNext() => _position < _allSeries.Count;

        public Series Next()
        {
            if (!HasNext()) throw new InvalidOperationException("No more series.");
            return _allSeries[_position++];
        }

        public void Reset() => _position = 0;
    }

    public class LibrarySearchIterator
    {
        private readonly List<Author> _authors;

        public LibrarySearchIterator(List<Author> authors) => _authors = authors;

        public List<Author> FindAuthorsByPrefix(string query)
        {
            var results = new List<Author>();
            var iterator = new AuthorIterator(_authors);

            while (iterator.HasNext())
            {
                var author = iterator.Next();
                if (author.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    results.Add(author);
            }

            return results;
        }

        public List<(Author Author, Book Book, Series? Series)> FindBooksByPrefix(string query)
        {
            var results = new List<(Author, Book, Series?)>();

            foreach (var author in _authors)
            {
                foreach (var book in author.Books)
                {
                    if (!book.IsMissing && book.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                        results.Add((author, book, null));
                }

                foreach (var series in author.Series)
                {
                    foreach (var book in series.Books)
                    {
                        if (!book.IsMissing && book.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                            results.Add((author, book, series));
                    }
                }
            }

            return results;
        }

        public List<(Author Author, Series Series)> FindSeriesByPrefix(string query)
        {
            var results = new List<(Author, Series)>();
            var seriesIterator = new SeriesIterator(_authors);
            seriesIterator.Reset();

            foreach (var author in _authors)
            {
                foreach (var series in author.Series)
                {
                    if (series.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                        results.Add((author, series));
                }
            }

            return results;
        }
    }
}
