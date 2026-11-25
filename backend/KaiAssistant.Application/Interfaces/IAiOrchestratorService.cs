using KaiAssistant.Application.AI;
namespace KaiAssistant.Application.Interfaces;

public interface IAiOrchestratorService
{
    Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default);
}