using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class ConversationOptions
{
    public const string SectionName = "Conversations";
    public bool Enabled { get; set; } = true;
    [Range(2, 100)]
    public int MaxMessages { get; set; } = 24;
    [Range(2, 50)]
    public int ShortTermWindow { get; set; } = 10;
    [Range(100, 8000)]
    public int SummaryMaxChars { get; set; } = 1200;
}