using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Mobile.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mobile/dashboard")]
public sealed class MobileDashboardController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IQueryHandler<GetMobileDashboardQuery, MobileDashboardDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetMobileDashboardQuery(currentUserService.GetRequiredUserId()),
            cancellationToken);

        return Ok(result);
    }
}