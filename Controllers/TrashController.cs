using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Interfaces;
using PersonalLibrary.Models;
using PersonalLibrary.Patterns;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class TrashController : BaseLibraryController
    {
        private readonly TrashService _trashService;
        private readonly ILibraryRepository _libraryRepository;

        public TrashController(TrashService trashService, LibraryEventPublisher publisher,
            LocalizationService loc, ILibraryRepository libraryRepository)
            : base(publisher, loc)
        {
            _trashService      = trashService;
            _libraryRepository = libraryRepository;
        }

        public IActionResult Index() =>
            View(new TrashViewModel { Items = _trashService.GetAll() });

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Restore(string id)
        {
            var item = _trashService.GetAll().FirstOrDefault(i => i.Id == id);
            if (item == null) return Json(new { success = false, message = _loc["Error_NotFound"] });

            var dupMsg = CheckDuplicate(item);
            if (dupMsg != null) return Json(new { success = false, message = dupMsg });

            var error = _trashService.Restore(id);
            if (error != null) return Json(new { success = false, message = _loc[error] });

            _publisher.ItemRestored(item.ItemType == TrashItemType.Book ? _loc.BookTitle(item.ItemName) : item.ItemName, item.ItemType.ToString());
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult DeleteForever(string id)
        {
            _trashService.DeletePermanently(id);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            _trashService.Clear();
            _publisher.TrashCleared();
            return Json(new { success = true });
        }

        private string? CheckDuplicate(TrashItem item)
        {
            var allAuthors = _libraryRepository.GetAllAuthors();
            switch (item.ItemType)
            {
                case TrashItemType.Author:
                    if (allAuthors.Any(a => a.Name.Equals(item.ItemName, StringComparison.OrdinalIgnoreCase)))
                        return string.Format(_loc["Trash_AlreadyExists_Author"], item.ItemName);
                    break;
                case TrashItemType.Series:
                    var a4s = allAuthors.FirstOrDefault(a => a.Id == item.AuthorId);
                    if (a4s != null && a4s.Series.Any(s => s.Name.Equals(item.ItemName, StringComparison.OrdinalIgnoreCase)))
                        return string.Format(_loc["Trash_AlreadyExists_Series"], item.ItemName);
                    break;
                case TrashItemType.Book:
                    var a4b = allAuthors.FirstOrDefault(a => a.Id == item.AuthorId);
                    var trashedBook = System.Text.Json.JsonSerializer.Deserialize<Book>(item.SerializedData,
                        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                    bool untitledMissing = trashedBook != null && trashedBook.IsMissing && string.IsNullOrWhiteSpace(trashedBook.Title);
                    if (a4b != null && !untitledMissing)
                    {
                        bool exists = a4b.Books.Any(b => b.Title.Equals(item.ItemName, StringComparison.OrdinalIgnoreCase))
                            || a4b.Series.Any(s => s.Books.Any(b => b.Title.Equals(item.ItemName, StringComparison.OrdinalIgnoreCase)));
                        if (exists)
                            return string.Format(_loc["Trash_AlreadyExists_Book"], item.ItemName);
                    }
                    break;
            }
            return null;
        }
    }
}
