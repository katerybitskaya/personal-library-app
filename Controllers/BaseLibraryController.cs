using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Patterns;
using PersonalLibrary.Services;

namespace PersonalLibrary.Controllers
{
    public abstract class BaseLibraryController : Controller
    {
        protected readonly LibraryEventPublisher _publisher;
        protected readonly LocalizationService _loc;

        protected BaseLibraryController(LibraryEventPublisher publisher, LocalizationService loc)
        {
            _publisher = publisher;
            _loc = loc;
        }
    }
}
