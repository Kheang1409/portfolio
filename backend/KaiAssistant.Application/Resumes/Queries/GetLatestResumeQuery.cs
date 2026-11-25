using KaiAssistant.Domain.Entities.Resumes;
using MediatR;
namespace KaiAssistant.Application.Resumes.Queries;

public record GetLatestResumeQuery() : IRequest<Resume?>;