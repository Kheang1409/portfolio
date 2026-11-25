using KaiAssistant.Domain.Entities.Outbox;
using KaiAssistant.Domain.Entities.Resumes;
namespace KaiAssistant.Application.Interfaces;

public interface IResumeWriteService
{
    Task<Resume> CreateWithOutboxAsync(
        Resume resume,
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default);
}