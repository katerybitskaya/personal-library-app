namespace PersonalLibrary.Models
{
    public class HistoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string MessageKey { get; set; } = string.Empty;

        public List<string> Params { get; set; } = new();

        public string Message { get; set; } = string.Empty;
    }
}
