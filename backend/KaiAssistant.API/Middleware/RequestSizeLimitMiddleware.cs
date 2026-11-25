using KaiAssistant.API.Options;
using Microsoft.Extensions.Options;
namespace KaiAssistant.API.Middleware;

public sealed class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestHardeningOptions _options;
    public RequestSizeLimitMiddleware(RequestDelegate next, IOptions<RequestHardeningOptions> options)
    {
        _next = next;
        _options = options.Value;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var max = Math.Max(1024, _options.MaxRequestBodySizeBytes);
        var contentLength = context.Request.ContentLength;
        if (contentLength.HasValue && contentLength.Value > max)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new { message = "Request payload too large." }).ConfigureAwait(false);
            return;
        }
        await _next(context).ConfigureAwait(false);
    }
}