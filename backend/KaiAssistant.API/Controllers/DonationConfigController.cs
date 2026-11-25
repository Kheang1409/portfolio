using KaiAssistant.Infrastructure.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using KaiAssistant.Domain.Entities;

namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("api/donations/config")]
public sealed class DonationConfigController(IOptions<PayPalOptions> options, EmailSettings emailSettings) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ClientSecret))
            return NotFound();
        return Ok(new { clientId = settings.ClientId, currency = settings.Currency, environment = settings.Environment, minimumDonation = settings.MinimumDonation, maximumDonation = settings.MaximumDonation, receiptAvailable = emailSettings.Enabled });
    }
}
