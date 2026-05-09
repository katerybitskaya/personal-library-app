using PersonalLibrary.Interfaces;
using PersonalLibrary.Models;

namespace PersonalLibrary.Patterns
{
    public class LibraryComposite : ILibraryComponent
    {
        public string Id { get; } = "library-root";
        public string Name { get; } = "Library";

        private readonly List<ILibraryComponent> _children = new();

        public void Add(ILibraryComponent component) => _children.Add(component);
        public void Remove(ILibraryComponent component) => _children.Remove(component);
        public IReadOnlyList<ILibraryComponent> GetChildren() => _children.AsReadOnly();

        public void Display(int depth)
        {
            Console.WriteLine(new string('=', depth) + Name);
            foreach (var child in _children)
                child.Display(depth + 2);
        }
    }

    public class AuthorComposite : ILibraryComponent
    {
        private readonly Author _author;
        private readonly List<ILibraryComponent> _children = new();

        public string Id => _author.Id;
        public string Name => _author.Name;

        public AuthorComposite(Author author)
        {
            _author = author;

            foreach (var book in author.Books)
                _children.Add(new BookComposite(book));

            foreach (var series in author.Series)
                _children.Add(new SeriesComposite(series));
        }

        public IReadOnlyList<ILibraryComponent> GetChildren() => _children.AsReadOnly();

        public void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + Name);
            foreach (var child in _children)
                child.Display(depth + 2);
        }
    }

    public class SeriesComposite : ILibraryComponent
    {
        private readonly Series _series;
        private readonly List<ILibraryComponent> _children = new();

        public string Id => _series.Id;
        public string Name => _series.Name;

        public SeriesComposite(Series series)
        {
            _series = series;
            foreach (var book in series.Books.OrderBy(b => b.OrderInSeries ?? int.MaxValue))
                _children.Add(new BookComposite(book));
        }

        public IReadOnlyList<ILibraryComponent> GetChildren() => _children.AsReadOnly();

        public void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + Name);
            foreach (var child in _children)
                child.Display(depth + 2);
        }
    }

    public class BookComposite : ILibraryComponent
    {
        private readonly Book _book;

        public string Id => _book.Id;
        public string Name => _book.Title;

        public BookComposite(Book book) => _book = book;

        public void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + Name);
        }
    }
}
