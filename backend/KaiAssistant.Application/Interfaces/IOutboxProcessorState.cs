using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Interfaces;

public interface IOutboxProcessorState
{
    OutboxProcessorSnapshot Snapshot { get; }
    void MarkCycleStarted();
    void MarkCycleSucceeded();
    void MarkCycleFailed(string error);
}