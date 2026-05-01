using MentoraX.Api.Contracts.Sync;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Mobile.Queries;
using MentoraX.Application.Features.StudyPlans.Queries;
using MentoraX.Application.Features.Sync.Commands;
using MentoraX.Application.Features.Sync.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sync")]
public sealed class SyncController : ControllerBase
{
    [HttpGet("bootstrap")]
    public async Task<IActionResult> Bootstrap(
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IQueryHandler<GetStudyPlansQuery, IReadOnlyCollection<StudyPlanDto>> studyPlansHandler,
        [FromServices] IQueryHandler<GetMobileDashboardQuery, MobileDashboardDto> dashboardHandler,
        [FromServices] IQueryHandler<GetNextStudySessionQuery, NextStudySessionDto?> nextSessionHandler,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var studyPlans = await studyPlansHandler.Handle(
            new GetStudyPlansQuery(userId),
            cancellationToken);
        var dashboard = await dashboardHandler.Handle(
            new GetMobileDashboardQuery(userId),
            cancellationToken);
        var nextSession = await nextSessionHandler.Handle(
            new GetNextStudySessionQuery(userId),
            cancellationToken);

        return Ok(new SyncBootstrapDto(
            DateTime.UtcNow,
            studyPlans,
            dashboard,
            nextSession));
    }

    [HttpGet("changes")]
    public async Task<IActionResult> Changes(
        [FromQuery] DateTime? since,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IQueryHandler<GetSyncChangesQuery, SyncChangesDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetSyncChangesQuery(
                currentUserService.GetRequiredUserId(),
                since ?? DateTime.MinValue),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("push")]
    public async Task<IActionResult> Push(
        [FromBody] SyncPushRequest request,
        [FromServices] ICommandHandler<PushSyncOperationsCommand, SyncPushResponseDto> handler,
        CancellationToken cancellationToken)
    {
        var operations = request.Operations
            .Select(x => x.ToDto())
            .ToList();

        var result = await handler.Handle(
            new PushSyncOperationsCommand(operations),
            cancellationToken);

        return Ok(result);
    }
}
