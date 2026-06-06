using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Api.Authorization;
using Nexus.Application.Abstractions;
using Nexus.Application.Engineering.Commands.GenerateStandup;
using Nexus.Application.Engineering.Queries.GetEngineeringActivity;
using Nexus.Application.Engineering.Commands.DeclareIncident;
using Nexus.Application.Engineering.Commands.ResolveIncident;
using Nexus.Application.Engineering.Queries.ListIncidents;
using Nexus.Api.Requests.Engineering;

namespace Nexus.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireEngineer)]
public class EngineeringController(ISender sender, ICurrentUser currentUser) : ApiController(sender)
{
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity()
    {
        return HandleResult(await Sender.Send(new GetEngineeringActivity.Query()));
    }

    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents([FromQuery] Guid workspaceId)
    {
        return HandleResult(await Sender.Send(new ListIncidents.Query(workspaceId)));
    }

    [HttpPost("standup")]
    public async Task<IActionResult> GenerateStandup([FromQuery] Guid? userId, [FromQuery] string? authorName)
    {
        // Use the query parameter if provided, otherwise fallback to the authenticated user.
        var targetUserId = userId ?? currentUser.Id;
        
        var result = await Sender.Send(new GenerateStandup.Command(UserId: targetUserId, AuthorName: authorName));
        
        return result.IsSuccess
            ? Ok(new { summary = result.Value })
            : BadRequest(new { message = result.Error });
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> DeclareIncident([FromBody] DeclareIncidentRequest request)
    {
        var result = await Sender.Send(new DeclareIncident.Command(
            request.Title,
            request.Description,
            request.Severity,
            currentUser.Id,
            request.WorkspaceId));

        return HandleResult(result);
    }

    [HttpPost("incidents/resolve")]
    public async Task<IActionResult> ResolveIncident([FromBody] ResolveIncidentRequest request)
    {
        var result = await Sender.Send(new ResolveIncident.Command(
            request.ChannelId,
            currentUser.Id));

        return HandleResult(result);
    }
}
