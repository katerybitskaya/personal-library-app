using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class FavoritesController : Controller
    {
        private readonly LibraryService _libraryService;
        private readonly LocalizationService _loc;

        public FavoritesController(LibraryService libraryService, LocalizationService loc)
        {
            _libraryService = libraryService;
            _loc = loc;
        }

        public IActionResult Index()
        {
            return View(new FavoritesViewModel
            {
                Authors = _libraryService.GetFavoriteAuthors(),
                Series  = _libraryService.GetFavoriteSeries(),
                Books   = _libraryService.GetFavoriteBooks()
            });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Toggle(SetFavoriteForm form)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = _loc["Error_Validation"] });
            var (success, message) = _libraryService.SetFavorite(form.Kind, form.Id, form.IsFavorite);
            if (!success) return Json(new { success = false, message = _loc[message] });
            return Json(new { success = true, isFavorite = form.IsFavorite });
        }
    }
}
