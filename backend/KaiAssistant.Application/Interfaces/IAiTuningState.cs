using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Options;
namespace KaiAssistant.Application.Interfaces;

public interface IAiTuningState
{
    AiTuningWeightsSnapshot GetCurrent(AiModelOrchestrationOptions baseline);
    void Update(AiTuningWeightsSnapshot snapshot);
}