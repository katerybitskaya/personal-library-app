using System.Text.Json;
using PersonalLibrary.Models;

namespace PersonalLibrary.Repositories
{
    public class JsonHistoryRepository
    {
        private readonly string _historyFilePath;
        private List<HistoryEntry> _entries;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonHistoryRepository(string dataDirectory)
        {
            _historyFilePath = Path.Combine(dataDirectory, "history.json");
            Directory.CreateDirectory(dataDirectory);
            _entries = LoadFromFile();
        }

        public List<HistoryEntry> GetAll() =>
            _entries.OrderByDescending(e => e.Timestamp).ToList();

        public void Add(HistoryEntry entry)
        {
            _entries.Add(entry);
            Save();
        }

        public void Clear()
        {
            _entries.Clear();
            Save();
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_entries, _jsonOptions);
            File.WriteAllText(_historyFilePath, json);
        }

        private List<HistoryEntry> LoadFromFile()
        {
            if (!File.Exists(_historyFilePath)) return new List<HistoryEntry>();

            try
            {
                var json = File.ReadAllText(_historyFilePath);
                return JsonSerializer.Deserialize<List<HistoryEntry>>(json, _jsonOptions) ?? new();
            }
            catch
            {
                return new List<HistoryEntry>();
            }
        }
    }
}
