using System.Text;
using KaiAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Http;
namespace KaiAssistant.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private const int MAX_REQUESTS = 100;
    private static readonly TimeSpan WINDOW = TimeSpan.FromMinutes(1);
    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(
        HttpContext context,
        IRateLimitingService rateLimiter,
        ILogger<RateLimitingMiddleware> logger)
    {
        var identifier = GetClientIdentifier(context);
        var (isAllowed, remaining, resetAt) = await rateLimiter.CheckRateLimitAsync(
        identifier,
        MAX_REQUESTS,
        WINDOW)
        .ConfigureAwait(false);
        // Set rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = MAX_REQUESTS.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetAt.ToUnixTimeSeconds().ToString();
        if (!isAllowed)
        {
            logger.LogWarning("Rate limit exceeded: Identifier={Identifier}, ResetAt={ResetAt}", identifier, resetAt);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfter = (resetAt - DateTimeOffset.UtcNow).TotalSeconds
            }).ConfigureAwait(false);
            return;
        }
        await _next(context).ConfigureAwait(false);
    }
    private static string GetClientIdentifier(HttpContext context)
    {
        // Prefer X-Forwarded-For (load balancer), fall back to RemoteIpAddress
        var ip = context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor)
            ? forwardedFor.ToString().Split(',')[0].Trim()
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        // Use IP as identifier; can also use session/user ID if available
        return ip;
    }
}
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
