using KaiAssistant.Application.Resumes.Queries;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Mvc;
namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("api/resumes")]
public class ResumeController : ControllerBase
{
    private readonly IMediator _mediator;
    public ResumeController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet("latest")]
    [OutputCache(Duration = 30)]
    public async Task<IActionResult> GetLatest(CancellationToken cancellationToken)
    {
        var resume = await _mediator.Send(new GetLatestResumeQuery(), cancellationToken).ConfigureAwait(false);
        if (resume == null) return NotFound();
        return Ok(resume);
    }
}
