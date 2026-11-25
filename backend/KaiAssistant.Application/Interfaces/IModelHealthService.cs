using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Interfaces;

public interface IModelHealthService
{
    bool CanAttempt(string modelName, DateTimeOffset utcNow);
    void RecordSuccess(string modelName, DateTimeOffset utcNow, bool fallbackUsed);
    void RecordFailure(string modelName, DateTimeOffset utcNow, string reason);
    void RecordLatency(string modelName, DateTimeOffset utcNow, double latencyMs);
    void RecordUsage(string modelName, DateTimeOffset utcNow, int inputTokens, int outputTokens, decimal estimatedCostUsd);
    void MarkRateLimited(string modelName, DateTimeOffset utcNow, DateTimeOffset? retryAtUtc, string reason);
    void EnsureModelsRegistered(IEnumerable<string> modelNames, DateTimeOffset utcNow);
    void SetActiveModel(string modelName, DateTimeOffset utcNow);
    string? GetActiveModel();
    long GetStateVersion();
    void FlushPendingChanges(DateTimeOffset utcNow);
    AiModelHealthSnapshot GetSnapshot(DateTimeOffset utcNow);
}