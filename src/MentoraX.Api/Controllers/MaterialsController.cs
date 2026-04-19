using MentoraX.Api.Contracts.Materials;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Materials.Commands;
using MentoraX.Application.Features.Materials.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/materials")]
public sealed class MaterialsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMaterialRequest request, [FromServices] ICurrentUserService currentUserService, [FromServices] ICommandHandler<CreateMaterialCommand, MaterialDto> handler, CancellationToken cancellationToken)
    {
        var command = new CreateMaterialCommand(request.Title, request.MaterialType, request.Content, request.EstimatedDurationMinutes, request.Description, request.Tags);
        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromServices] ICurrentUserService currentUserService, [FromServices] IQueryHandler<GetMaterialsQuery, IReadOnlyCollection<MaterialDto>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMaterialsQuery(currentUserService.GetRequiredUserId()), cancellationToken);
        return Ok(result);
    }
}
