using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.Infrastructure.Governance;

public sealed class DefaultClientContextAccessor : IClientContextAccessor
{
    public string GetClientIp() => "unknown";
}