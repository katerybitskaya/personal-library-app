using System.Text.Json;
using PersonalLibrary.Models;

namespace PersonalLibrary.Repositories
{
    public class JsonTrashRepository
    {
        private readonly string _trashFilePath;
        private List<TrashItem> _items;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonTrashRepository(string dataDirectory)
        {
            _trashFilePath = Path.Combine(dataDirectory, "trash.json");
            Directory.CreateDirectory(dataDirectory);
            _items = LoadFromFile();
        }

        public List<TrashItem> GetAll() => _items.OrderByDescending(i => i.DeletedAt).ToList();

        public TrashItem? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);

        public void Add(TrashItem item)
        {
            _items.Add(item);
            Save();
        }

        public void Remove(string id)
        {
            var item = GetById(id);
            if (item != null)
            {
                _items.Remove(item);
                Save();
            }
        }

        public void Clear()
        {
            _items.Clear();
            Save();
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_items, _jsonOptions);
            File.WriteAllText(_trashFilePath, json);
        }

        private List<TrashItem> LoadFromFile()
        {
            if (!File.Exists(_trashFilePath)) return new List<TrashItem>();

            try
            {
                var json = File.ReadAllText(_trashFilePath);
                return JsonSerializer.Deserialize<List<TrashItem>>(json, _jsonOptions) ?? new();
            }
            catch
            {
                return new List<TrashItem>();
            }
        }
    }
}
