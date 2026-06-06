using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Workspaces.Queries.GetWorkspaces;
using Nexus.Application.Workspaces.Commands.CreateWorkspace;

namespace Nexus.Api.Controllers;

[Authorize]
public class WorkspacesController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    public async Task<IActionResult> GetWorkspaces()
    {
        return HandleResult(await Sender.Send(new GetWorkspaces.Query()));
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspace.Command command)
    {
        return HandleResult(await Sender.Send(command));
    }
}
