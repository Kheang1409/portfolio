using KaiAssistant.Application.Contacts.Commands;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
namespace KaiAssistant.Application.Contacts.Handlers;

public record ContactCommandHandler : IRequestHandler<ContactCommand>
{
    private readonly IEmailService _service;
    private readonly ILogger<ContactCommandHandler> _logger;
    public ContactCommandHandler(
        IEmailService service,
        ILogger<ContactCommandHandler> logger)
    {
        _service = service;
        _logger = logger;
    }
    public async Task Handle(ContactCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing contact message from {Email} with name {Name}.", command.Email, command.Name);
        try
        {
            // Send notification to portfolio owner
            await _service.SendContactEmailAsync(command.Name, command.Email, command.Message, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Contact email sent successfully for {Email}.", command.Email);
            // Send confirmation email to sender
            await _service.SendConfirmationEmailAsync(command.Name, command.Email, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Confirmation email sent successfully to {Email}.", command.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact or confirmation email for {Email}.", command.Email);
            throw;
        }
    }
}
