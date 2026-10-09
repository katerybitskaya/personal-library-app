using PersonalLibrary.Interfaces;
using PersonalLibrary.Models;
using PersonalLibrary.Patterns;
using PersonalLibrary.Repositories;

namespace PersonalLibrary.Services
{
    public class HistoryService : ILibraryObserver
    {
        private readonly JsonHistoryRepository _repository;

        public HistoryService(JsonHistoryRepository repository)
        {
            _repository = repository;
        }

        public void OnEvent(string eventMessage)
        {
            var parts = eventMessage.Split(LibraryEventPublisher.Separator);
            var entry = new HistoryEntry
            {
                Timestamp  = DateTime.Now,
                MessageKey = parts[0],
                Params     = parts.Skip(1).ToList(),
                Message    = string.Empty  
            };
            _repository.Add(entry);
        }

        public List<HistoryEntry> GetAll() => _repository.GetAll();
        public void Clear() => _repository.Clear();
    }
}
