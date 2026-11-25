using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class ModelHealthBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IModelHealthService _modelHealth;
    private readonly IOptions<GeminiSettings> _gemini;
    private readonly ILogger<ModelHealthBootstrapHostedService> _logger;
    public ModelHealthBootstrapHostedService(
        IServiceProvider services,
        IModelHealthService modelHealth,
        IOptions<GeminiSettings> gemini,
        ILogger<ModelHealthBootstrapHostedService> logger)
    {
        _services = services;
        _modelHealth = modelHealth;
        _gemini = gemini;
        _logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var models = (_gemini.Value.ModelNames ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (models.Count == 0)
        {
            return;
        }
        _modelHealth.EnsureModelsRegistered(models, DateTimeOffset.UtcNow);
        using var scope = _services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<IAiModelGateway>();
        foreach (var model in models)
        {
            try
            {
                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = "health check" } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        maxOutputTokens = 8,
                        candidateCount = 1
                    }
                };
                var result = await gateway.SendRequestAsync(model, payload, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    _modelHealth.RecordSuccess(model, DateTimeOffset.UtcNow, fallbackUsed: false);
                    _modelHealth.SetActiveModel(model, DateTimeOffset.UtcNow);
                    _logger.LogInformation("AI model health check passed for {Model}", model);
                }
                else
                {
                    _modelHealth.RecordFailure(model, DateTimeOffset.UtcNow, "empty_response");
                    _logger.LogWarning("AI model health check returned empty response for {Model}", model);
                }
            }
            catch (Exception ex)
            {
                _modelHealth.RecordFailure(model, DateTimeOffset.UtcNow, ex.Message);
                _logger.LogWarning(ex, "AI model health check failed for {Model}", model);
            }
        }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}