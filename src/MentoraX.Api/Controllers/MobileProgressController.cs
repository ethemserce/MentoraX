using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Mobile.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mobile/progress")]
public sealed class MobileProgressController : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IQueryHandler<GetMobileProgressSummaryQuery, MobileProgressSummaryDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetMobileProgressSummaryQuery(currentUserService.GetRequiredUserId()),
            cancellationToken);

        return Ok(result);
    }
}