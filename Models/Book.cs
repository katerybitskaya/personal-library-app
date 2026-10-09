using System.Text.Json.Serialization;
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

        public List<BookFlag> Flags { get; set; } = new();

        public string? Note { get; set; }

        public bool IsFavorite { get; set; }

        public string Name => Title;

        public void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + Title);
        }

        public bool IsMissing => Title.Trim() == "-";

        [JsonIgnore]
        public List<BookFlag> ActiveFlags => Flags.Where(f => BookFlags.AllowedFor(this).Contains(f)).ToList();

        [JsonIgnore]
        public bool IsFavoriteActive => IsFavorite && !IsMissing;

        public bool HasFlags => ActiveFlags.Count > 0;
    }
}
