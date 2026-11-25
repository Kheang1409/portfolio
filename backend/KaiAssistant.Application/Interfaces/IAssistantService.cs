using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface IAssistantService
{
    IAsyncEnumerable<AiStreamChunk> StreamAsync(
        string question,
        ConversationMessage[]? history = null,
        AssistantContext? context = null,
        CancellationToken cancellationToken = default);
}