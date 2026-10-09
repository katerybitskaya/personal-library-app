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
            var authors = _libraryService.GetAllAuthors();
            var vm = new CatalogueViewModel
            {
                AuthorsByLetter = _libraryService.GetAuthorsByLetter(),
                ExistingLetters = _libraryService.GetExistingLetters(),
                AuthorCount = authors.Count,
                SeriesCount = authors.Sum(a => a.Series.Count),
                BookCount = authors.Sum(a => a.Books.Count(b => !b.IsMissing)
                                           + a.Series.Sum(s => s.Books.Count(b => !b.IsMissing)))
            };
            if (!string.IsNullOrWhiteSpace(q))
            {
                vm.SearchQuery = q.Trim();
                vm.SearchResult = _libraryService.Search(vm.SearchQuery);
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusPage(int code)
        {
            if (code != StatusCodes.Status404NotFound) return StatusCode(code);
            Response.StatusCode = code;
            return View("PageNotFound");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StorageError()
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View();
        }
    }
}
