namespace KaiAssistant.Application.Interfaces;

using KaiAssistant.Application.Diagnostics;
public interface IAiModelGateway
{
    Task<(string? Body, string? UsedModel)> SendGenerationRequestAsync(string payloadJson, IEnumerable<string> models, CancellationToken cancellationToken = default);
    Task<string?> SendRequestAsync(string modelName, object payload, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AiGatewayStreamChunk> StreamGenerationRequestAsync(
        string payloadJson,
        IEnumerable<string> models,
        int maxDurationSeconds,
        int maxTokens,
        CancellationToken cancellationToken = default);
}