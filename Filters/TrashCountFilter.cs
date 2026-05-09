using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PersonalLibrary.Services;

namespace PersonalLibrary.Filters
{
    public class TrashCountFilter : IActionFilter
    {
        private readonly TrashService _trashService;

        public TrashCountFilter(TrashService trashService)
        {
            _trashService = trashService;
        }

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Controller is Controller controller)
            {
                controller.ViewBag.TrashCount = _trashService.GetAll().Count;
            }
        }
    }
}
