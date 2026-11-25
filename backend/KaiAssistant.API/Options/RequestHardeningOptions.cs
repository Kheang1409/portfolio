using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.API.Options;

public sealed class RequestHardeningOptions
{
    public const string SectionName = "RequestHardening";
    [Range(1024, 5_242_880)]
    public long MaxRequestBodySizeBytes { get; set; } = 262144;
}