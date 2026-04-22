using MentoraX.Api.Contracts.Auth;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, [FromServices] ICommandHandler<RegisterCommand, AuthResponseDto> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RegisterCommand(request.FullName, request.Email, request.Password, request.TimeZone), cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, [FromServices] ICommandHandler<LoginCommand, AuthResponseDto> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(result);
    }
}
