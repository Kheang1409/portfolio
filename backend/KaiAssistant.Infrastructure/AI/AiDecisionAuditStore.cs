using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
namespace KaiAssistant.Infrastructure.AI;

public sealed class AiDecisionAuditStore : IAiDecisionAuditStore
{
    private readonly object _sync = new();
    private readonly Queue<AiDecisionAuditEntry> _entries = new();
    public void Record(AiDecisionAuditEntry entry)
    {
        lock (_sync)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > 500)
            {
                _entries.Dequeue();
            }
        }
    }
    public IReadOnlyList<AiDecisionAuditEntry> GetRecent(int maxEntries = 100)
    {
        var bounded = Math.Clamp(maxEntries, 1, 500);
        lock (_sync)
        {
            return _entries.Reverse().Take(bounded).ToList();
        }
    }
}