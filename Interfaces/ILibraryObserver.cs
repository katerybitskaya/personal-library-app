namespace PersonalLibrary.Interfaces
{
    public interface ILibraryObserver
    {
        void OnEvent(string eventMessage);
    }
}
