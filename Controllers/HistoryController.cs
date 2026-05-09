using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Patterns;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;

namespace PersonalLibrary.Controllers
{
    public class HistoryController : BaseLibraryController
    {
        private readonly HistoryService _historyService;

        public HistoryController(HistoryService historyService, LibraryEventPublisher publisher, LocalizationService loc)
            : base(publisher, loc)
        {
            _historyService = historyService;
        }

        public IActionResult Index() =>
            View(new HistoryViewModel { Entries = _historyService.GetAll() });

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            _historyService.Clear();
            return Json(new { success = true });
        }
    }
}
