using KaiAssistant.Application.AI;
namespace KaiAssistant.Application.Interfaces;

public interface IAiProvider
{
    string ProviderName { get; }
    Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken);
}