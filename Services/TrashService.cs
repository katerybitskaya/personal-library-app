using System.Text.Json;
using PersonalLibrary.Models;
using PersonalLibrary.Interfaces;
using PersonalLibrary.Repositories;

namespace PersonalLibrary.Services
{
    public class TrashService
    {
        private readonly JsonTrashRepository _trashRepository;
        private readonly ILibraryRepository _libraryRepository;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TrashService(JsonTrashRepository trashRepository, ILibraryRepository libraryRepository)
        {
            _trashRepository = trashRepository;
            _libraryRepository = libraryRepository;
        }

        public List<TrashItem> GetAll() => _trashRepository.GetAll();

        public void MoveBookToTrash(string authorId, string bookId)
        {
            var author = _libraryRepository.GetAuthorById(authorId);
            var book = _libraryRepository.GetBookById(bookId);
            if (author == null || book == null) return;

            Series? parentSeries = author.Series.FirstOrDefault(s => s.Books.Any(b => b.Id == bookId));

            var trashItem = new TrashItem
            {
                ItemType     = TrashItemType.Book,
                ItemName     = book.Title,
                AuthorName   = author.Name,
                AuthorId     = authorId,
                SeriesName   = parentSeries?.Name,
                SeriesId     = parentSeries?.Id,
                SerializedData = JsonSerializer.Serialize(book, _jsonOptions)
            };

            _trashRepository.Add(trashItem);
            _libraryRepository.DeleteBook(authorId, bookId);
        }

        public void MoveSeriesToTrash(string authorId, string seriesId)
        {
            var author = _libraryRepository.GetAuthorById(authorId);
            var series = _libraryRepository.GetSeriesById(seriesId);
            if (author == null || series == null) return;

            var trashItem = new TrashItem
            {
                ItemType     = TrashItemType.Series,
                ItemName     = series.Name,
                AuthorName   = author.Name,
                AuthorId     = authorId,
                SerializedData = JsonSerializer.Serialize(series, _jsonOptions)
            };

            _trashRepository.Add(trashItem);
            _libraryRepository.DeleteSeries(authorId, seriesId);
        }

        public void MoveAuthorToTrash(string authorId)
        {
            var author = _libraryRepository.GetAuthorById(authorId);
            if (author == null) return;

            var trashItem = new TrashItem
            {
                ItemType     = TrashItemType.Author,
                ItemName     = author.Name,
                AuthorName   = author.Name,
                AuthorId     = authorId,
                SerializedData = JsonSerializer.Serialize(author, _jsonOptions)
            };

            _trashRepository.Add(trashItem);
            _libraryRepository.DeleteAuthor(authorId);
        }

        public bool Restore(string trashItemId)
        {
            var item = _trashRepository.GetById(trashItemId);
            if (item == null) return false;

            try
            {
                switch (item.ItemType)
                {
                    case TrashItemType.Author:
                    {
                        var restoredAuthor = JsonSerializer.Deserialize<Author>(item.SerializedData, _jsonOptions);
                        if (restoredAuthor != null)
                        {
                            foreach (var b in restoredAuthor.Books)
                                b.AuthorId = restoredAuthor.Id;
                            foreach (var s in restoredAuthor.Series)
                            {
                                s.AuthorId = restoredAuthor.Id;
                                foreach (var b in s.Books)
                                {
                                    b.AuthorId = restoredAuthor.Id;
                                    b.SeriesId = s.Id;
                                }
                            }
                            _libraryRepository.AddAuthor(restoredAuthor);
                        }
                        break;
                    }

                    case TrashItemType.Series:
                    {
                        var restoredSeries = JsonSerializer.Deserialize<Series>(item.SerializedData, _jsonOptions);
                        if (restoredSeries != null)
                        {
                            var existingAuthor = _libraryRepository.GetAuthorById(item.AuthorId);
                            if (existingAuthor == null) return false;
                            _libraryRepository.AddSeries(item.AuthorId, restoredSeries);
                        }
                        break;
                    }

                    case TrashItemType.Book:
                    {
                        var restoredBook = JsonSerializer.Deserialize<Book>(item.SerializedData, _jsonOptions);
                        if (restoredBook != null)
                        {
                            var existingAuthor = _libraryRepository.GetAuthorById(item.AuthorId);
                            if (existingAuthor == null) return false;

                            if (!string.IsNullOrEmpty(item.SeriesId))
                            {
                                var existingSeries = _libraryRepository.GetSeriesById(item.SeriesId);
                                if (existingSeries != null)
                                    _libraryRepository.AddBookToSeries(item.SeriesId, restoredBook);
                                else
                                    _libraryRepository.AddBook(item.AuthorId, restoredBook);
                            }
                            else
                            {
                                _libraryRepository.AddBook(item.AuthorId, restoredBook);
                            }
                        }
                        break;
                    }
                }

                _trashRepository.Remove(trashItemId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void DeletePermanently(string trashItemId) => _trashRepository.Remove(trashItemId);

        public void Clear() => _trashRepository.Clear();
    }
}
