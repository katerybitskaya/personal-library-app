using System.Text.Json.Serialization;
using PersonalLibrary.Interfaces;

namespace PersonalLibrary.Models
{
    public class Book : ILibraryComponent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;

        public int? OrderInSeries { get; set; }

        public string? SeriesNumber { get; set; }

        public string? CoverPath { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public string AuthorId { get; set; } = string.Empty;

        public string? SeriesId { get; set; }

        public List<BookFlag> Flags { get; set; } = new();

        public string? Note { get; set; }

        public List<string> FavoriteBy { get; set; } = new();

        [JsonPropertyName("isFavorite")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool LegacyIsFavorite { get; set; }

        [JsonIgnore]
        public string Name => Title;

        public void Display(int depth)
        {
            Console.WriteLine(new string('-', depth) + Title);
        }

        public bool IsMissing { get; set; }

        public void NormalizeMissing()
        {
            if (IsMissing) FavoriteBy.Clear();
            Flags = ActiveFlags;
        }

        [JsonIgnore]
        public List<BookFlag> ActiveFlags => Flags.Where(f => BookFlags.AllowedFor(this).Contains(f)).ToList();

        [JsonIgnore]
        public string DisplayNumber => string.IsNullOrWhiteSpace(SeriesNumber) ? OrderInSeries?.ToString() ?? "·" : SeriesNumber;

        [JsonIgnore]
        public List<string> ActiveFavoriteBy => IsMissing ? new List<string>() : FavoriteBy;

        [JsonIgnore]
        public bool HasFlags => ActiveFlags.Count > 0;
    }
}
