using KaiAssistant.Application.Contacts.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContactController> _logger;
    public ContactController(IMediator mediator, ILogger<ContactController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ContactCommand command, CancellationToken cancellationToken)
    {
        if (command == null)
        {
            _logger.LogWarning("ContactCommand received null payload.");
            return BadRequest(new { error = "Invalid contact request." });
        }
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Message))
        {
            _logger.LogWarning("ContactCommand received incomplete data. Name: {NameEmpty}, Email: {EmailEmpty}, Message: {MessageEmpty}",
                string.IsNullOrWhiteSpace(command.Name),
                string.IsNullOrWhiteSpace(command.Email),
                string.IsNullOrWhiteSpace(command.Message));
            return BadRequest(new { error = "Name, email, and message are required." });
        }
        try
        {
            _logger.LogInformation("Processing contact submission from {Email}.", command.Email);
            await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Contact submission processed successfully for {Email}.", command.Email);
            return Ok(new { message = "Email sent successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process contact submission from {Email}.", command.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to send email. Please try again later." });
        }
    }
}