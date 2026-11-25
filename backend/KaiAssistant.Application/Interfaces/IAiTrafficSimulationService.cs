using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Interfaces;

public interface IAiTrafficSimulationService
{
    Task<AiSimulationRunReport> RunOnceAsync(int? requestCountOverride, CancellationToken cancellationToken = default);
    IReadOnlyList<AiSimulationMetricSnapshot> GetSnapshots(int maxEntries = 120);
    AiSimulationRunReport? GetLastReport();
}