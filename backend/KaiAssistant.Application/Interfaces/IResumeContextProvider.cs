using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface IResumeContextProvider
{
    Task<ResumeChunk[]> GetResumeChunksAsync(CancellationToken cancellationToken = default);
    Task<ResumeChunk[]> GetRelevantChunksAsync(string question, CancellationToken cancellationToken = default);
}