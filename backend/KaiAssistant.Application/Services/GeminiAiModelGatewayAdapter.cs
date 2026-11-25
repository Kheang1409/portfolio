using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Services;

public class GeminiAiModelGatewayAdapter : IAiModelGateway
{
    private readonly IGeminiGateway _gemini;
    public GeminiAiModelGatewayAdapter(IGeminiGateway gemini)
    {
        _gemini = gemini;
    }
    public Task<(string? Body, string? UsedModel)> SendGenerationRequestAsync(string payloadJson, IEnumerable<string> models, CancellationToken cancellationToken = default)
    {
        return _gemini.SendGenerationRequestAsync(payloadJson, models, cancellationToken);
    }
    public async Task<string?> SendRequestAsync(string modelName, object payload, CancellationToken cancellationToken = default)
    {
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
        var result = await _gemini.SendGenerationRequestAsync(payloadJson, new[] { modelName }, cancellationToken).ConfigureAwait(false);
        return result.Body;
    }
    public IAsyncEnumerable<AiGatewayStreamChunk> StreamGenerationRequestAsync(
        string payloadJson,
        IEnumerable<string> models,
        int maxDurationSeconds,
        int maxTokens,
        CancellationToken cancellationToken = default)
    {
        return _gemini.StreamGenerationRequestAsync(payloadJson, models, maxDurationSeconds, maxTokens, cancellationToken);
    }
}