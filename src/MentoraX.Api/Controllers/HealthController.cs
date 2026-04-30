// src/MentoraX.Api/Controllers/HealthController.cs

using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "MentoraX.Api",
            utcNow = DateTime.UtcNow
        });
    }
}