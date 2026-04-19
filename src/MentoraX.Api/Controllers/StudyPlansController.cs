using MentoraX.Api.Contracts.StudyPlans;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.StudyPlans.Commands;
using MentoraX.Application.Features.StudyPlans.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/study-plans")]
public sealed class StudyPlansController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudyPlanRequest request, [FromServices] ICurrentUserService currentUserService, [FromServices] ICommandHandler<CreateStudyPlanCommand, StudyPlanDto> handler, CancellationToken cancellationToken)
    {
        var command = new CreateStudyPlanCommand(currentUserService.GetRequiredUserId(), request.LearningMaterialId, request.Title, request.StartDate, request.DailyTargetMinutes, request.PreferredHour, request.DayOffsets);
        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromServices] IQueryHandler<GetStudyPlanByIdQuery, StudyPlanDto?> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetStudyPlanByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
