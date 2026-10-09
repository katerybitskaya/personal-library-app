using PersonalLibrary.Interfaces;

namespace PersonalLibrary.Patterns
{

    public class LibraryEventPublisher : ILibrarySubject
    {
        private readonly List<ILibraryObserver> _observers = new();

        public void Subscribe(ILibraryObserver o)   { if (!_observers.Contains(o)) _observers.Add(o); }
        public void Unsubscribe(ILibraryObserver o)  => _observers.Remove(o);

        public const char Separator = '\u001F';
        private static string Msg(params string[] parts) => string.Join(Separator, parts);
        private static string Title(string title) => string.IsNullOrWhiteSpace(title) ? "Book_Untitled" : title;

        public void NotifyObservers(string msg)
        {
            foreach (var o in _observers) o.OnEvent(msg);
        }

        public void AuthorAdded(string name)                                  => NotifyObservers(Msg("Hist_AuthorAdded", name));
        public void AuthorDeleted(string name)                                => NotifyObservers(Msg("Hist_AuthorDeleted", name));
        public void AuthorPhotoUpdated(string name)                           => NotifyObservers(Msg("Hist_AuthorPhoto", name));
        public void BookAdded(string title, string author, string? series)    => NotifyObservers(series != null ? Msg("Hist_BookAddedSeries", Title(title), author, series) : Msg("Hist_BookAdded", Title(title), author));
        public void BookDeleted(string title, string author, string? series)  => NotifyObservers(series != null ? Msg("Hist_BookDeletedSeries", Title(title), author, series) : Msg("Hist_BookDeleted", Title(title), author));
        public void BookRenamed(string o, string n, string author, string? s) => NotifyObservers(s != null ? Msg("Hist_BookRenamedSeries", Title(o), Title(n), author, s) : Msg("Hist_BookRenamed", Title(o), Title(n), author));
        public void BookCoverUpdated(string title, string author)             => NotifyObservers(Msg("Hist_BookCover", Title(title), author));
        public void SeriesAdded(string name, string author)                   => NotifyObservers(Msg("Hist_SeriesAdded", name, author));
        public void SeriesDeleted(string name, string author)                 => NotifyObservers(Msg("Hist_SeriesDeleted", name, author));
        public void SeriesRenamed(string o, string n, string author)          => NotifyObservers(Msg("Hist_SeriesRenamed", o, n, author));
        public void SeriesCoverUpdated(string name, string author)            => NotifyObservers(Msg("Hist_SeriesCover", name, author));
        public void ItemRestored(string name, string type)
        {
            var typeKey = type switch { "Author" => "Label_Author", "Series" => "Label_Series", _ => "Label_Book" };
            NotifyObservers(Msg("Hist_Restored", typeKey, type == "Book" ? Title(name) : name));
        }
        public void TrashCleared()                                            => NotifyObservers("Hist_TrashCleared");
        public void HistoryCleared()                                          => NotifyObservers("Hist_HistoryCleared");
    }

}
