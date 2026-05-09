namespace PersonalLibrary.Interfaces
{
    public interface ILibrarySubject
    {
        void Subscribe(ILibraryObserver observer);
        void Unsubscribe(ILibraryObserver observer);
        void NotifyObservers(string eventMessage);
    }
}
