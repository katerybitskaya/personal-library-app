using System.Text.Json;
using PersonalLibrary.Models;

namespace PersonalLibrary.Repositories
{
    public class JsonUserRepository
    {
        private const int MaxLogEntries = 200;

        private readonly string _filePath;
        private readonly object _saveLock = new();
        private readonly UsersData _data;

        public string? LoadError { get; }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonUserRepository(string dataDirectory)
        {
            _filePath = Path.Combine(dataDirectory, "users.json");
            Directory.CreateDirectory(dataDirectory);
            try
            {
                _data = JsonFileStore.Load<UsersData>(_filePath, _jsonOptions);
            }
            catch (StorageException ex)
            {
                _data = new UsersData();
                LoadError = ex.Message;
            }
        }

        public List<AppUser> GetAll() => _data.Users.ToList();

        public AppUser? GetById(string? id) => id == null ? null : _data.Users.FirstOrDefault(u => u.Id == id);

        public AppUser? GetByLogin(string login) =>
            _data.Users.FirstOrDefault(u => u.Login.Equals(login.Trim(), StringComparison.OrdinalIgnoreCase));

        public void Add(AppUser user)
        {
            _data.Users.Add(user);
            Save();
        }

        public void Remove(string id)
        {
            _data.Users.RemoveAll(u => u.Id == id);
            Save();
        }

        public void AddLog(IEnumerable<ProfileLogEntry> entries)
        {
            _data.ProfileLog.AddRange(entries);
            if (_data.ProfileLog.Count > MaxLogEntries)
                _data.ProfileLog.RemoveRange(0, _data.ProfileLog.Count - MaxLogEntries);
        }

        public List<ProfileLogEntry> GetLog() => _data.ProfileLog.OrderByDescending(e => e.Timestamp).ToList();

        public DateTime? AdminLogSeenAt
        {
            get => _data.AdminLogSeenAt;
            set => _data.AdminLogSeenAt = value;
        }

        public void Save()
        {
            if (LoadError != null)
                throw new StorageException("users.json was not loaded; saving is disabled to protect the data.");
            lock (_saveLock)
                JsonFileStore.Save(_filePath, _data, _jsonOptions);
        }
    }
}
