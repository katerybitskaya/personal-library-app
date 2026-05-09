namespace PersonalLibrary.Interfaces
{
    public interface ILibraryComponent
    {
        string Id { get; }
        string Name { get; }
        void Display(int depth);
    }
}
