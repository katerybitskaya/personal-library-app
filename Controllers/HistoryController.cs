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

        private const int PageSize = 30;

        public IActionResult Index(int page = 1)
        {
            var all = _historyService.GetAll();
            var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            return View(new HistoryViewModel
            {
                Entries    = all.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
                TotalCount = all.Count,
                Page       = page,
                PageSize   = PageSize
            });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            _historyService.Clear();
            return Json(new { success = true });
        }
    }
}
