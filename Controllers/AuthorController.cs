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

        public AuthorController(LibraryService libraryService, TrashService trashService,
            LibraryEventPublisher publisher, LocalizationService loc, IWebHostEnvironment env)
            : base(publisher, loc)
        {
            _libraryService = libraryService;
            _trashService   = trashService;
            _env            = env;
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
                return Json(new { success = false, message = _loc["Author_AlreadyExists"] });

            var photoPath = await FileUploadHelper.SaveAsync(form.PhotoFile, form.PhotoPath, _env);
            var author = new Author { Name = form.Name, PhotoPath = photoPath };

            var (success, message) = _libraryService.AddAuthor(author);
            if (!success) return Json(new { success = false, message });

            _publisher.AuthorAdded(author.Name);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhoto(UpdatePhotoForm form)
        {
            var path = await FileUploadHelper.SaveAsync(form.PhotoFile, form.Path, _env);
            if (string.IsNullOrEmpty(path))
                return Json(new { success = false, message = "No image provided." });

            _libraryService.UpdateAuthorPhoto(form.Id, path);
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
