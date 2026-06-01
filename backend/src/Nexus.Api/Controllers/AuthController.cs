using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Auth.Commands.Login;
using Nexus.Application.Auth.Commands.Register;

namespace Nexus.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUser.Command command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUser.Command command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
