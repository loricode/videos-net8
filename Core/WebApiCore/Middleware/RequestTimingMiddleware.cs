using System.Diagnostics;

namespace WebApiCore.Middleware
{
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestTimingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Response-Time-ms"] =
                    sw.ElapsedMilliseconds.ToString();

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
