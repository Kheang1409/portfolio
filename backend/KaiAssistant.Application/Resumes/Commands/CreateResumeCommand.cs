using KaiAssistant.Domain.Entities.Resumes;
using MediatR;
namespace KaiAssistant.Application.Resumes.Commands;

public record CreateResumeCommand(Resume Resume) : IRequest<Resume>;