namespace PersonalLibrary.Middleware
{
    public class RequestLockMiddleware
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private readonly RequestDelegate _next;

        public RequestLockMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _lock.WaitAsync(context.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await _next(context);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
