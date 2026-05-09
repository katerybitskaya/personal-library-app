using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryService _libraryService;

        public HomeController(LibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        public IActionResult Index(string? q)
        {
            var vm = new CatalogueViewModel
            {
                AuthorsByLetter = _libraryService.GetAuthorsByLetter(),
                ExistingLetters = _libraryService.GetExistingLetters()
            };
            if (!string.IsNullOrWhiteSpace(q))
            {
                vm.SearchQuery = q;
                vm.SearchResult = _libraryService.Search(q);
            }
            return View(vm);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            var allowed = new[] { "en", "ru", "pl" };
            if (!allowed.Contains(culture)) culture = "en";

            Response.Cookies.Append("lang", culture, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
