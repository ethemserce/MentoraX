using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.StudyPlans.Queries;
using MentoraX.Application.Features.StudyProgress.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/study-progress")]
    public sealed class StudyProgressController : ControllerBase
    {

        [HttpGet("{materialId:guid}")]
        public async Task<IActionResult> Get(Guid materialId, [FromServices] IQueryHandler<GetStudyProgressQuery, StudyProgressDto?> handler, CancellationToken cancellationToken)
        {
            var result = await handler.Handle(
                new GetStudyProgressQuery(materialId),
                cancellationToken);

            if (result is null)
                return NotFound();

            return Ok(result);
        }
    }
}
