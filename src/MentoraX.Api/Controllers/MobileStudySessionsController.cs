using MentoraX.Api.Contracts.Mobile;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Mobile.Commands;
using MentoraX.Application.Features.Mobile.Queries;
using MentoraX.Application.Features.StudySessions.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mobile/study-sessions")]
public sealed class MobileStudySessionsController : ControllerBase
{
    [HttpGet("next")]
    public async Task<IActionResult> GetNext(
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IQueryHandler<GetNextStudySessionQuery, NextStudySessionDto?> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetNextStudySessionQuery(currentUserService.GetRequiredUserId()),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("{sessionId:guid}/start")]
    public async Task<IActionResult> Start(
        Guid sessionId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ICommandHandler<StartStudySessionCommand, NextStudySessionDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new StartStudySessionCommand(sessionId),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{sessionId:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid sessionId,
        [FromBody] CompleteMobileStudySessionRequest request,
        [FromServices] ICommandHandler<CompleteStudySessionCommand, StudySessionDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CompleteStudySessionCommand
            (
                 sessionId,
                 request.QualityScore,
                 request.DifficultyScore,
                 request.ActualDurationMinutes,
                 request.ReviewNotes
            ),
            cancellationToken);

        return Ok(result);
    }
}