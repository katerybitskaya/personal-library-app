using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Helpers;
using PersonalLibrary.Models;
using PersonalLibrary.Patterns;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class AuthorController : BaseLibraryController
    {
        private readonly LibraryService _libraryService;
        private readonly TrashService _trashService;
        private readonly IWebHostEnvironment _env;

        private readonly UploadCleanupService _uploads;

        public AuthorController(LibraryService libraryService, TrashService trashService,
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
            var author = _libraryService.GetAuthorById(id);
            if (author == null) return NotFound();
            return View(new AuthorViewModel { Author = author });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddAuthorForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Name))
                return Json(new { success = false, message = _loc["Author_NameRequired"] });

            if (FileUploadHelper.IsRejected(form.PhotoFile))
                return Json(new { success = false, message = _loc["Error_NoImage"] });

            var photoPath = await FileUploadHelper.SaveAsync(form.PhotoFile, _env);
            var author = new Author { Name = form.Name, PhotoPath = photoPath };

            var (success, message) = _libraryService.AddAuthor(author);
            if (!success)
            {
                _uploads.DeleteIfUnused(new[] { photoPath });
                return Json(new { success = false, message = _loc[message] });
            }

            _publisher.AuthorAdded(author.Name);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhoto(UpdatePhotoForm form)
        {
            if (_libraryService.GetAuthorById(form.Id) == null)
                return Json(new { success = false, message = _loc["Error_NotFound"] });
            var path = await FileUploadHelper.SaveAsync(form.PhotoFile, _env);
            if (string.IsNullOrEmpty(path))
                return Json(new { success = false, message = _loc["Error_NoImage"] });

            _libraryService.UpdateAuthorPhoto(form.Id, path);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Rename(RenameForm form)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = _loc["Error_Validation"] });
            var (success, message) = _libraryService.RenameAuthor(form.Id, form.NewName);
            if (!success) return Json(new { success = false, message = _loc[message] });
            _trashService.RenameAuthor(form.Id, form.NewName.Trim());
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            var author = _libraryService.GetAuthorById(id);
            if (author == null) return Json(new { success = false, message = _loc["Error_NotFound"] });

            _trashService.MoveAuthorToTrash(id);
            _publisher.AuthorDeleted(author.Name);
            return Json(new { success = true });
        }
    }
}
