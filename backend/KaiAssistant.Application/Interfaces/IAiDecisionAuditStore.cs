using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Interfaces;

public interface IAiDecisionAuditStore
{
    void Record(AiDecisionAuditEntry entry);
    IReadOnlyList<AiDecisionAuditEntry> GetRecent(int maxEntries = 100);
}