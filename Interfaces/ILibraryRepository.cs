using PersonalLibrary.Models;

namespace PersonalLibrary.Interfaces
{
    public interface ILibraryRepository
    {
        List<Author> GetAllAuthors();
        Author? GetAuthorById(string id);
        void AddAuthor(Author author);
        void UpdateAuthor(Author author);
        void DeleteAuthor(string authorId);

        Book? GetBookById(string id);
        void AddBook(string authorId, Book book);
        void UpdateBook(Book book);
        void DeleteBook(string authorId, string bookId);

        Series? GetSeriesById(string id);
        void AddSeries(string authorId, Series series);
        void UpdateSeries(Series series);
        void DeleteSeries(string authorId, string seriesId);

        void AddBookToSeries(string seriesId, Book book);

        void Save();

        string? LoadError { get; }
    }
}
