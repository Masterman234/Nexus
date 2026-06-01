using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Workspaces.Queries.GetWorkspaces;
using Nexus.Application.Workspaces.Commands.CreateWorkspace;

namespace Nexus.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/workspaces")]
public class WorkspacesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWorkspaces()
    {
        var result = await sender.Send(new GetWorkspaces.Query());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspace.Command command)
    {
        var result = await sender.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
