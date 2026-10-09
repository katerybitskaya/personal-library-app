using System.Text.Json.Serialization;
using PersonalLibrary.Interfaces;

namespace PersonalLibrary.Models
{
    public class Author : ILibraryComponent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;

        public string? PhotoPath { get; set; }

        public bool IsFavorite { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public List<Book> Books { get; set; } = new();

        public List<Series> Series { get; set; } = new();

        public void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + Name);

            foreach (var book in Books)
                book.Display(depth + 2);

            foreach (var series in Series)
                series.Display(depth + 2);
        }

        [JsonIgnore]
        public char FirstLetter => char.ToUpper(Name.FirstOrDefault());
    }
}
