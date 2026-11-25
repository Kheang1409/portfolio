using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface IAiPromptBuilder
{
    string BuildSystemPrompt();
    List<object> BuildContents(string resumeContext, string question);
    object BuildGenerationConfig(string question);
    string NormalizeInput(string input);
    string NormalizeContextBlock(AssistantContext? context);
    string ComputePromptHash(string question, ConversationMessage[]? history, string resumeContext, AssistantContext? context);
}