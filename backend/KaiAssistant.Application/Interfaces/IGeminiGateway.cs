using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Interfaces;

public interface IGeminiGateway
{
    Task<(string? Body, string? UsedModel)> SendGenerationRequestAsync(string payloadJson, IEnumerable<string> models, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AiGatewayStreamChunk> StreamGenerationRequestAsync(
        string payloadJson,
        IEnumerable<string> models,
        int maxDurationSeconds,
        int maxTokens,
        CancellationToken cancellationToken = default);
}