using KaiAssistant.Application.Resumes.Commands;
using KaiAssistant.Application.Events;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities.Resumes;
using MediatR;
namespace KaiAssistant.Application.Resumes.Handlers;

public class CreateResumeHandler : IRequestHandler<CreateResumeCommand, Resume>
{
    private readonly IResumeWriteService _resumeWriteService;
    public CreateResumeHandler(IResumeWriteService resumeWriteService)
    {
        _resumeWriteService = resumeWriteService;
    }
    public async Task<Resume> Handle(CreateResumeCommand request, CancellationToken cancellationToken)
    {
        request.Resume.CreatedAtUtc = DateTimeOffset.UtcNow;
        var integrationEvent = new ResumeCreatedIntegrationEvent(request.Resume.Id);
        var outboxMessage = OutboxMessageFactory.Create(integrationEvent);
        return await _resumeWriteService
            .CreateWithOutboxAsync(request.Resume, outboxMessage, cancellationToken)
            .ConfigureAwait(false);
    }
}