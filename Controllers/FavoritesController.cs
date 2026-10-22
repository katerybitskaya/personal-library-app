using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class FavoritesController : Controller
    {
        public const string AllUsers = "all";

        private readonly LibraryService _libraryService;
        private readonly LocalizationService _loc;
        private readonly CurrentUserService _me;
        private readonly UserService _users;

        public FavoritesController(LibraryService libraryService, LocalizationService loc, CurrentUserService me, UserService users)
        {
            _libraryService = libraryService;
            _loc = loc;
            _me = me;
            _users = users;
        }

        public IActionResult Index(string? user)
        {
            var selected = !_me.ProtectionEnabled || string.IsNullOrEmpty(user) || (user != AllUsers && _users.GetById(user) == null)
                ? _me.Id
                : user;
            var filterId = selected == AllUsers ? null : selected;
            var counts = _libraryService.CountFavoritesByUser();

            return View(new FavoritesViewModel
            {
                Authors      = _libraryService.GetFavoriteAuthors(filterId),
                Series       = _libraryService.GetFavoriteSeries(filterId),
                Books        = _libraryService.GetFavoriteBooks(filterId),
                SelectedUser = selected,
                ShowUsers    = _me.ProtectionEnabled,
                Users        = _users.GetAll()
                    .Where(u => u.Id == _me.Id || counts.ContainsKey(u.Id))
                    .Select(u => (u, counts.GetValueOrDefault(u.Id)))
                    .ToList(),
                AllCount     = _libraryService.CountFavoriteItems()
            });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Toggle(SetFavoriteForm form)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = _loc["Error_Validation"] });
            var (success, message) = _libraryService.SetFavorite(form.Kind, form.Id, form.IsFavorite, _me.Id);
            if (!success) return Json(new { success = false, message = _loc[message] });
            return Json(new { success = true, isFavorite = form.IsFavorite });
        }
    }
}
