using KaiAssistant.Application.Donations;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("api/donations")]
public sealed class DonationsController(IDonationService donations) : ControllerBase
{
    [HttpPost("orders")]
    [ProducesResponseType(typeof(CreateDonationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateDonationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await donations.CreateOrderAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { message = "This donation request is already being processed or could not be started. Please retry shortly." });
        }
    }

    [HttpPost("capture")]
    [ProducesResponseType(typeof(DonationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Capture([FromBody] CaptureDonationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await donations.CaptureAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Donation order was not found." });
        }
        catch (ArgumentException)
        {
            return BadRequest(new { message = "Invalid donation order." });
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { message = "PayPal has not confirmed this donation." });
        }
    }

    [HttpPost("webhook")]
    [RequestSizeLimit(262144)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var headers = new PayPalWebhookHeaders(
                Request.Headers["PayPal-Transmission-Id"], Request.Headers["PayPal-Transmission-Time"],
                Request.Headers["PayPal-Cert-Url"], Request.Headers["PayPal-Auth-Algo"], Request.Headers["PayPal-Transmission-Sig"]);
            return await donations.HandleWebhookAsync(headers, rawBody, cancellationToken).ConfigureAwait(false) ? Ok() : Unauthorized();
        }
        catch (JsonException) { return BadRequest(); }
        catch (InvalidOperationException) { return StatusCode(StatusCodes.Status503ServiceUnavailable); }
    }

    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(DonorLeaderboardResult), StatusCodes.Status200OK)]
    public async Task<DonorLeaderboardResult> GetLeaderboard(CancellationToken cancellationToken) =>
        await donations.GetLeaderboardAsync(cancellationToken).ConfigureAwait(false);
}
