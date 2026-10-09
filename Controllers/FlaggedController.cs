using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class FlaggedController : Controller
    {
        private readonly LibraryService _libraryService;

        public FlaggedController(LibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        public IActionResult Index()
        {
            return View(new FlaggedViewModel
            {
                OngoingSeries = _libraryService.GetOngoingSeries(),
                Books = _libraryService.GetFlaggedBooks()
            });
        }
    }
}
