using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Helpers;
using PersonalLibrary.Models;
using PersonalLibrary.Patterns;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class BookController : BaseLibraryController
    {
        private readonly LibraryService _libraryService;
        private readonly TrashService _trashService;
        private readonly IWebHostEnvironment _env;

        private readonly UploadCleanupService _uploads;

        public BookController(LibraryService libraryService, TrashService trashService,
            LibraryEventPublisher publisher, LocalizationService loc, IWebHostEnvironment env, UploadCleanupService uploads)
            : base(publisher, loc)
        {
            _libraryService = libraryService;
            _trashService   = trashService;
            _env            = env;
            _uploads        = uploads;
        }

        public IActionResult Details(string id)
        {
            var book = _libraryService.GetBookById(id);
            if (book == null) return NotFound();
            var author = _libraryService.GetAuthorById(book.AuthorId);
            if (author == null) return NotFound();
            Series? parentSeries = !string.IsNullOrEmpty(book.SeriesId)
                ? _libraryService.GetSeriesById(book.SeriesId) : null;
            return View(new BookViewModel { Book = book, Author = author, ParentSeries = parentSeries });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddBookForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Title))
                return Json(new { success = false, message = _loc["Error_TitleRequired"] });

            if (FileUploadHelper.IsRejected(form.CoverFile))
                return Json(new { success = false, message = _loc["Error_NoImage"] });

            var coverPath = await FileUploadHelper.SaveAsync(form.CoverFile, form.CoverPath, _env);
            var book = new Book { Title = form.Title, CoverPath = coverPath };

            var (success, message) = _libraryService.AddBook(form.AuthorId, book);
            if (!success)
            {
                _uploads.DeleteIfUnused(new[] { coverPath });
                return Json(new { success = false, message = _loc[message] });
            }

            _publisher.BookAdded(book.Title, _libraryService.GetAuthorById(form.AuthorId)?.Name ?? "", null);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Rename(RenameForm form)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = _loc["Error_Validation"] });
            var (success, message) = _libraryService.RenameBook(form.Id, form.NewName);
            if (!success) return Json(new { success = false, message = _loc[message] });
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult UpdateFlags(UpdateFlagsForm form)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = _loc["Error_Validation"] });
            var (success, message) = _libraryService.UpdateBookFlags(form.Id, form.Flags, form.Note, form.IsMissing);
            if (!success) return Json(new { success = false, message = _loc[message] });
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCover(UpdatePhotoForm form)
        {
            var path = await FileUploadHelper.SaveAsync(form.PhotoFile, form.Path, _env);
            if (string.IsNullOrEmpty(path)) return Json(new { success = false, message = _loc["Error_NoImage"] });
            _libraryService.UpdateBookCover(form.Id, path);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Delete(string bookId, string authorId)
        {
            var book   = _libraryService.GetBookById(bookId);
            var author = _libraryService.GetAuthorById(authorId);
            if (book == null || author == null) return Json(new { success = false, message = _loc["Error_NotFound"] });

            var series = author.Series.FirstOrDefault(s => s.Books.Any(b => b.Id == bookId));
            _trashService.MoveBookToTrash(authorId, bookId);
            _publisher.BookDeleted(_loc.BookTitle(book.Title), author.Name, series?.Name);
            return Json(new { success = true });
        }
    }
}
