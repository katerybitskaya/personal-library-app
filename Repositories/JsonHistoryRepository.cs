using System.Text.Json;
using PersonalLibrary.Models;

namespace PersonalLibrary.Repositories
{
    public class JsonHistoryRepository
    {
        private readonly string _historyFilePath;
        private readonly object _saveLock = new();
        private List<HistoryEntry> _entries;

        public string? LoadError { get; }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonHistoryRepository(string dataDirectory)
        {
            _historyFilePath = Path.Combine(dataDirectory, "history.json");
            Directory.CreateDirectory(dataDirectory);
            try
            {
                _entries = JsonFileStore.Load<List<HistoryEntry>>(_historyFilePath, _jsonOptions);
            }
            catch (StorageException ex)
            {
                _entries = new List<HistoryEntry>();
                LoadError = ex.Message;
            }
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
            if (LoadError != null)
                throw new StorageException("history.json was not loaded; saving is disabled to protect the data.");
            lock (_saveLock)
                JsonFileStore.Save(_historyFilePath, _entries, _jsonOptions);
        }
    }
}
