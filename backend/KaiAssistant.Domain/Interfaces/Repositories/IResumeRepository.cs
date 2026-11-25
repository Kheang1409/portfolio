using KaiAssistant.Domain.Entities.Resumes;
namespace KaiAssistant.Domain.Interfaces.Repositories;

public interface IResumeRepository
{
    Task<Resume?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Resume?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task InsertAsync(Resume resume, CancellationToken cancellationToken = default);
}