using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.API.Options;

public sealed class OutboxRecoveryOptions
{
    public const string SectionName = "OutboxRecovery";
    public bool EnableReplayEndpoints { get; set; } = false;
    [Range(1, 1000)]
    public int MaxReplayBatchSize { get; set; } = 100;
}