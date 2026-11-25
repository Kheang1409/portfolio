using Microsoft.AspNetCore.Mvc;
namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpHead]
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}