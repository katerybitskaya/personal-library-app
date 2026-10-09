using System.Text.Json;
using PersonalLibrary.Models;

namespace PersonalLibrary.Repositories
{
    public class JsonTrashRepository
    {
        private readonly string _trashFilePath;
        private readonly object _saveLock = new();
        private List<TrashItem> _items;

        public string? LoadError { get; }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonTrashRepository(string dataDirectory)
        {
            _trashFilePath = Path.Combine(dataDirectory, "trash.json");
            Directory.CreateDirectory(dataDirectory);
            try
            {
                _items = JsonFileStore.Load<List<TrashItem>>(_trashFilePath, _jsonOptions);
            }
            catch (StorageException ex)
            {
                _items = new List<TrashItem>();
                LoadError = ex.Message;
            }
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

        // После переименования автора — новое имя у его книг и серий в корзине
        public void RenameAuthor(string authorId, string newName)
        {
            var items = _items.Where(i => i.AuthorId == authorId && i.ItemType != TrashItemType.Author).ToList();
            if (items.Count == 0) return;
            items.ForEach(i => i.AuthorName = newName);
            Save();
        }

        public void Clear()
        {
            _items.Clear();
            Save();
        }

        private void Save()
        {
            if (LoadError != null)
                throw new StorageException("trash.json was not loaded; saving is disabled to protect the data.");
            lock (_saveLock)
                JsonFileStore.Save(_trashFilePath, _items, _jsonOptions);
        }
    }
}
