using MentoraX.Api.Contracts.StudyPlans;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.StudyPlans.Commands;
using MentoraX.Application.Features.StudyPlans.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/study-plans")]
public sealed class StudyPlansController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStudyPlanRequest request,
        [FromServices] ICommandHandler<CreateStudyPlanCommand, StudyPlanDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateStudyPlanCommand(
            request.LearningMaterialId,
            request.Title,
            request.StartDate,
            request.DailyTargetMinutes,
            request.PreferredHour,
            request.DayOffsets);

        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPlans(
    [FromServices] ICurrentUserService currentUserService,
    [FromServices] IQueryHandler<GetStudyPlansQuery, IReadOnlyCollection<StudyPlanDto>> handler,
    CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var result = await handler.Handle(
            new GetStudyPlansQuery(userId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetStudyPlanByIdQuery, StudyPlanDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetStudyPlanByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id,[FromServices] ICommandHandler<PauseStudyPlanCommand, int> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new PauseStudyPlanCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, [FromServices] ICommandHandler<ResumeStudyPlanCommand, int> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new ResumeStudyPlanCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromServices] ICommandHandler<CancelStudyPlanCommand, int> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new CancelStudyPlanCommand(id),cancellationToken);
        return NoContent();
    }
}