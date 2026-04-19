using MentoraX.Api.Contracts.StudySessions;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.StudySessions.Commands;
using MentoraX.Application.Features.StudySessions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/study-sessions")]
public sealed class StudySessionsController : ControllerBase
{
    [HttpGet("due")]
    public async Task<IActionResult> GetDueSessions([FromQuery] DateTime? untilUtc, [FromServices] ICurrentUserService currentUserService, [FromServices] IQueryHandler<GetDueStudySessionsQuery, IReadOnlyCollection<StudySessionDto>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetDueStudySessionsQuery(currentUserService.GetRequiredUserId(), untilUtc), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{sessionId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid sessionId, [FromBody] CompleteStudySessionRequest request, [FromServices] ICommandHandler<CompleteStudySessionCommand, StudySessionDto> handler, CancellationToken cancellationToken)
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
            cancellationToken); return Ok(result);
    }
}