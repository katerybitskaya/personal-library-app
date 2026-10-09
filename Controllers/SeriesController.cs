using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Helpers;
using PersonalLibrary.Models;
using PersonalLibrary.Patterns;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class SeriesController : BaseLibraryController
    {
        private readonly LibraryService _libraryService;
        private readonly TrashService _trashService;
        private readonly IWebHostEnvironment _env;

        public SeriesController(LibraryService libraryService, TrashService trashService,
            LibraryEventPublisher publisher, LocalizationService loc, IWebHostEnvironment env)
            : base(publisher, loc)
        {
            _libraryService = libraryService;
            _trashService   = trashService;
            _env            = env;
        }

        public IActionResult Details(string id)
        {
            var series = _libraryService.GetSeriesById(id);
            if (series == null) return NotFound();
            var author = _libraryService.GetAuthorById(series.AuthorId);
            if (author == null) return NotFound();
            return View(new SeriesViewModel { Series = series, Author = author });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Add(AddSeriesForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Name))
                return Json(new { success = false, message = "Name is required." });

            var series = new Series { Name = form.Name };
            int order = 1;
            foreach (var part in form.Parts.Where(p => !string.IsNullOrWhiteSpace(p.Title)))
                series.Books.Add(new Book { Title = part.Title.Trim(), OrderInSeries = order++ });

            var (success, message) = _libraryService.AddSeries(form.AuthorId, series);
            if (!success) return Json(new { success = false, message });

            _publisher.SeriesAdded(series.Name, _libraryService.GetAuthorById(form.AuthorId)?.Name ?? "");
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Rename(RenameForm form)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Validation failed." });
            var (success, message) = _libraryService.RenameSeries(form.Id, form.NewName);
            if (!success) return Json(new { success = false, message });
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult SetOngoing(SetOngoingForm form)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Validation failed." });
            var (success, message) = _libraryService.SetSeriesOngoing(form.Id, form.IsOngoing);
            if (!success) return Json(new { success = false, message });
            return Json(new { success = true, isOngoing = form.IsOngoing });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCover(UpdatePhotoForm form)
        {
            var path = await FileUploadHelper.SaveAsync(form.PhotoFile, form.Path, _env);
            if (string.IsNullOrEmpty(path)) return Json(new { success = false, message = "No image provided." });
            _libraryService.UpdateSeriesCover(form.Id, path);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook(AddBookToSeriesForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Title) && !form.IsMissing)
                return Json(new { success = false, message = "Title is required." });

            var coverPath = await FileUploadHelper.SaveAsync(form.CoverFile, form.CoverPath, _env);
            var book = new Book { Title = form.Title ?? string.Empty, OrderInSeries = form.OrderInSeries, CoverPath = coverPath, IsMissing = form.IsMissing };

            var (success, message) = _libraryService.AddBookToSeries(form.SeriesId, book);
            if (!success) return Json(new { success = false, message });

            var series = _libraryService.GetSeriesById(form.SeriesId);
            _publisher.BookAdded(book.Title, _libraryService.GetAuthorById(series?.AuthorId ?? "")?.Name ?? "", series?.Name);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Delete(string seriesId, string authorId)
        {
            var series = _libraryService.GetSeriesById(seriesId);
            var author = _libraryService.GetAuthorById(authorId);
            if (series == null || author == null) return Json(new { success = false, message = _loc["Error_NotFound"] });

            _trashService.MoveSeriesToTrash(authorId, seriesId);
            _publisher.SeriesDeleted(series.Name, author.Name);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult DeleteBook(string bookId, string seriesId)
        {
            var series = _libraryService.GetSeriesById(seriesId);
            if (series == null) return Json(new { success = false, message = _loc["Error_NotFound"] });
            var book = series.Books.FirstOrDefault(b => b.Id == bookId);
            if (book == null) return Json(new { success = false, message = _loc["Error_NotFound"] });

            var author = _libraryService.GetAuthorById(series.AuthorId);
            _trashService.MoveBookToTrash(series.AuthorId, bookId);
            _publisher.BookDeleted(book.Title, author?.Name ?? "", series.Name);
            return Json(new { success = true });
        }
    }
}
