using MediatR;
namespace KaiAssistant.Application.Contacts.Commands;

public record ContactCommand(
    string Name,
     string Email,
     string Message
) : IRequest;