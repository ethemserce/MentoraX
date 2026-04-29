using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.MaterialChunks.Commands;
using MentoraX.Application.Features.MaterialChunks.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/materials/{materialId:guid}/chunks")]
public sealed class MaterialChunksController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByMaterialId(
        Guid materialId,
        [FromServices] IQueryHandler<GetMaterialChunksQuery, IReadOnlyCollection<MaterialChunkDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetMaterialChunksQuery(materialId),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{chunkId:guid}")]
    public async Task<IActionResult> Update(
    Guid materialId,
    Guid chunkId,
    [FromBody] UpdateMaterialChunkRequest request,
    [FromServices] ICommandHandler<UpdateMaterialChunkCommand, MaterialChunkDto> handler,
    CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateMaterialChunkCommand(
                materialId,
                chunkId,
                request.Title,
                request.Content,
                request.Summary,
                request.Keywords,
                request.DifficultyLevel,
                request.EstimatedStudyMinutes
            ),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
    Guid materialId,
    [FromBody] CreateMaterialChunkRequest request,
    [FromServices] ICommandHandler<CreateMaterialChunkCommand, MaterialChunkDto> handler,
    CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateMaterialChunkCommand(
                materialId,
                request.Title,
                request.Content,
                request.Summary,
                request.Keywords,
                request.DifficultyLevel,
                request.EstimatedStudyMinutes
            ),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{chunkId:guid}")]
    public async Task<IActionResult> Delete(
    Guid materialId,
    Guid chunkId,
    [FromServices] ICommandHandler<DeleteMaterialChunkCommand, int> handler,
    CancellationToken cancellationToken)
    {
        await handler.Handle(
            new DeleteMaterialChunkCommand(materialId, chunkId),
            cancellationToken);

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(
    Guid materialId,
    [FromBody] ReorderMaterialChunksRequest request,
    [FromServices] ICommandHandler<ReorderMaterialChunksCommand, IReadOnlyCollection<MaterialChunkDto>> handler,
    CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ReorderMaterialChunksCommand(materialId, request.ChunkIds),
            cancellationToken);

        return Ok(result);
    }
}