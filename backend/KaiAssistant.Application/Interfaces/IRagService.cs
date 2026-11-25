using KaiAssistant.Application.AI;
namespace KaiAssistant.Application.Interfaces;

public interface IRagService
{
    Task<RagContextResult> BuildAugmentedPromptAsync(string question, CancellationToken cancellationToken = default);
}