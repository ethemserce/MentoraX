using MentoraX.Api.Contracts.Mobile;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Mobile.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MentoraX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mobile/devices")]
public sealed class MobileDevicesController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterMobileDeviceRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ICommandHandler<RegisterMobileDeviceCommand, MobileDeviceDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RegisterMobileDeviceCommand(
                request.DeviceToken,
                request.Platform),
            cancellationToken);

        return Ok(result);
    }
}