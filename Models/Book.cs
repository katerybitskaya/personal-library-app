using PersonalLibrary.Interfaces;

namespace PersonalLibrary.Models
{
    public class Book : ILibraryComponent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;

        public int? OrderInSeries { get; set; }

        public string? CoverPath { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public string AuthorId { get; set; } = string.Empty;

        public string? SeriesId { get; set; }

        public string Name => Title;

        public void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + Title);
        }

        public bool IsMissing => Title.Trim() == "-";
    }
}
