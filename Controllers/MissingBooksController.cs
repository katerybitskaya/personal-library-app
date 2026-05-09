using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class MissingBooksController : Controller
    {
        private readonly LibraryService _libraryService;

        public MissingBooksController(LibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        public IActionResult Index()
        {
            return View(new MissingBooksViewModel
            {
                Items = _libraryService.GetMissingBooks()
            });
        }
    }
}