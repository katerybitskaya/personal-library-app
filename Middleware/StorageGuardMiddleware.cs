using PersonalLibrary.Interfaces;
using PersonalLibrary.Repositories;

namespace PersonalLibrary.Middleware
{
    public class StorageGuardMiddleware
    {
        public const string ErrorPath = "/Home/StorageError";

        private readonly RequestDelegate _next;

        public StorageGuardMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ILibraryRepository library,
            JsonTrashRepository trash, JsonHistoryRepository history)
        {
            bool failed = library.LoadError != null || trash.LoadError != null || history.LoadError != null;
            if (failed && !context.Request.Path.StartsWithSegments("/Home/SetLanguage"))
            {
                context.Request.Path = ErrorPath;
                context.Request.QueryString = QueryString.Empty;
                context.Request.Method = HttpMethods.Get;
            }
            await _next(context);
        }
    }
}
