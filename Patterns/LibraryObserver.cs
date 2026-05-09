using PersonalLibrary.Interfaces;

namespace PersonalLibrary.Patterns
{

    public class LibraryEventPublisher : ILibrarySubject
    {
        private readonly List<ILibraryObserver> _observers = new();

        public void Subscribe(ILibraryObserver o)   { if (!_observers.Contains(o)) _observers.Add(o); }
        public void Unsubscribe(ILibraryObserver o)  => _observers.Remove(o);

        public void NotifyObservers(string msg)
        {
            foreach (var o in _observers) o.OnEvent(msg);
        }

        public void AuthorAdded(string name)                                  => NotifyObservers($"Hist_AuthorAdded|{name}");
        public void AuthorDeleted(string name)                                => NotifyObservers($"Hist_AuthorDeleted|{name}");
        public void AuthorPhotoUpdated(string name)                           => NotifyObservers($"Hist_AuthorPhoto|{name}");
        public void BookAdded(string title, string author, string? series)    => NotifyObservers(series != null ? $"Hist_BookAddedSeries|{title}|{author}|{series}" : $"Hist_BookAdded|{title}|{author}");
        public void BookDeleted(string title, string author, string? series)  => NotifyObservers(series != null ? $"Hist_BookDeletedSeries|{title}|{author}|{series}" : $"Hist_BookDeleted|{title}|{author}");
        public void BookRenamed(string o, string n, string author, string? s) => NotifyObservers(s != null ? $"Hist_BookRenamedSeries|{o}|{n}|{author}|{s}" : $"Hist_BookRenamed|{o}|{n}|{author}");
        public void BookCoverUpdated(string title, string author)             => NotifyObservers($"Hist_BookCover|{title}|{author}");
        public void SeriesAdded(string name, string author)                   => NotifyObservers($"Hist_SeriesAdded|{name}|{author}");
        public void SeriesDeleted(string name, string author)                 => NotifyObservers($"Hist_SeriesDeleted|{name}|{author}");
        public void SeriesRenamed(string o, string n, string author)          => NotifyObservers($"Hist_SeriesRenamed|{o}|{n}|{author}");
        public void SeriesCoverUpdated(string name, string author)            => NotifyObservers($"Hist_SeriesCover|{name}|{author}");
        public void ItemRestored(string name, string type)
        {
            var typeKey = type switch { "Author" => "Label_Author", "Series" => "Label_Series", _ => "Label_Book" };
            NotifyObservers($"Hist_Restored|{typeKey}|{name}");
        }
        public void TrashCleared()                                            => NotifyObservers("Hist_TrashCleared");
        public void HistoryCleared()                                          => NotifyObservers("Hist_HistoryCleared");
    }

    public class HistoryObserver : ILibraryObserver
    {
        private readonly Action<string> _logAction;
        public HistoryObserver(Action<string> logAction) => _logAction = logAction;
        public void OnEvent(string eventMessage) => _logAction(eventMessage);
    }
}
