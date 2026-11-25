using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.API.Services;

public sealed class HttpClientContextAccessor : IClientContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public HttpClientContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public string GetClientIp()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return "unknown";
        }
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}