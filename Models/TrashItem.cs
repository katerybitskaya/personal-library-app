namespace PersonalLibrary.Models
{
    public enum TrashItemType
    {
        Book,
        Series,
        Author
    }

    public class TrashItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public TrashItemType ItemType { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public string AuthorId { get; set; } = string.Empty;

        public DateTime DeletedAt { get; set; } = DateTime.Now;

        public string SerializedData { get; set; } = string.Empty;

        public string? SeriesName { get; set; }

        public string? SeriesId { get; set; }
    }
}
