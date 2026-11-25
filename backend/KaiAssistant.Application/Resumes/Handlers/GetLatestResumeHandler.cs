using KaiAssistant.Application.Resumes.Queries;
using KaiAssistant.Domain.Interfaces.Repositories;
using KaiAssistant.Domain.Entities.Resumes;
using MediatR;
namespace KaiAssistant.Application.Resumes.Handlers;

public class GetLatestResumeHandler : IRequestHandler<GetLatestResumeQuery, Resume?>
{
    private readonly IResumeRepository _repository;
    public GetLatestResumeHandler(IResumeRepository repository)
    {
        _repository = repository;
    }
    public async Task<Resume?> Handle(GetLatestResumeQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetLatestAsync(cancellationToken).ConfigureAwait(false);
    }
}