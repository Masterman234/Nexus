using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Engineering.Commands.GenerateStandup;
using Nexus.Application.Engineering.Queries.GetEngineeringActivity;

namespace Nexus.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/engineering")]
public class EngineeringController(ISender sender) : ControllerBase
{
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity()
    {
        var result = await sender.Send(new GetEngineeringActivity.Query());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("standup")]
    public async Task<IActionResult> GenerateStandup([FromQuery] string? authorName)
    {
        var result = await sender.Send(new GenerateStandup.Command(authorName));
        // Return error as `{ message }` so the frontend can read response.data.message
        // (matches what EngineeringTimeline.tsx already expects in its onError handler).
        return result.IsSuccess
            ? Ok(new { summary = result.Value })
            : BadRequest(new { message = result.Error });
    }
}
