using PersonalLibrary.Interfaces;

namespace PersonalLibrary.Models
{
    public class Series : ILibraryComponent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? CoverPath { get; set; }  
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public string AuthorId { get; set; } = string.Empty;
        public List<Book> Books { get; set; } = new();

        public void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + Name);
            foreach (var book in Books.OrderBy(b => b.OrderInSeries ?? int.MaxValue))
                book.Display(depth + 2);
        }

        public void AddBookToSeries(Book newBook)
        {
            if (newBook.OrderInSeries.HasValue)
            {
                int order = newBook.OrderInSeries.Value;
                if (order > Books.Count + 1)
                    newBook.OrderInSeries = Books.Count + 1;
                else
                    foreach (var book in Books.Where(b => b.OrderInSeries >= order))
                        book.OrderInSeries++;
            }
            else
            {
                newBook.OrderInSeries = Books.Count + 1;
            }
            newBook.SeriesId = Id;
            Books.Add(newBook);
        }
    }
}
