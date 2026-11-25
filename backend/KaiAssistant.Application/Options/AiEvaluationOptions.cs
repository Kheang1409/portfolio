using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class AiEvaluationOptions
{
    public const string SectionName = "AiEvaluation";
    [Range(1, 200)]
    public int MaxPromptsPerRun { get; set; } = 20;
}