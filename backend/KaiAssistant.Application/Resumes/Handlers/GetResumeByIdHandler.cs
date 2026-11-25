using KaiAssistant.Application.Resumes.Queries;
using KaiAssistant.Domain.Interfaces.Repositories;
using KaiAssistant.Domain.Entities.Resumes;
using MediatR;
namespace KaiAssistant.Application.Resumes.Handlers;

public class GetResumeByIdHandler : IRequestHandler<GetResumeByIdQuery, Resume?>
{
    private readonly IResumeRepository _repository;
    public GetResumeByIdHandler(IResumeRepository repository)
    {
        _repository = repository;
    }
    public async Task<Resume?> Handle(GetResumeByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
    }
}