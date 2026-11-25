using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Interfaces;

public interface IResilienceStatusProvider
{
    ResilienceSnapshot GetSnapshot();
    void RecordFailure(string component, string reason);
    void RecordSuccess(string component);
    void RecordCircuitState(string component, string state);
}