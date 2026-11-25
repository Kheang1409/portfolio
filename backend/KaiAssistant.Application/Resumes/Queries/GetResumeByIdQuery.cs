using KaiAssistant.Domain.Entities.Resumes;
using MediatR;
namespace KaiAssistant.Application.Resumes.Queries;

public record GetResumeByIdQuery(string Id) : IRequest<Resume?>;