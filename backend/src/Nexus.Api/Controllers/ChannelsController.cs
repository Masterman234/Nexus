using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Channels.Commands.SendMessage;
using Nexus.Application.Channels.Queries.GetChannels;
using Nexus.Application.Channels.Commands.EditMessage;
using Nexus.Application.Channels.Commands.DeleteMessage;
using Nexus.Application.Channels;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;

namespace Nexus.Api.Controllers;

public record SendMessageRequest(string Content, Guid UserId);
public record EditMessageRequest(string Content, Guid UserId);

[ApiController]
[Route("api/v{version:apiVersion}/channels")]
public class ChannelsController(ISender sender, IApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetChannels([FromQuery] Guid? workspaceId)
    {
        var result = await sender.Send(new GetChannels.Query(workspaceId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id)
    {
        var messages = await dbContext.Messages
            .Where(m => m.ChannelId == id)
            .OrderBy(m => m.SentAt)
            .Join(dbContext.Users,
                m => m.UserId,
                u => u.Id,
                (m, u) => new { m, u })
            .Select(x => new MessageResponse(
                x.m.Id,
                x.m.Content,
                x.u.Username,
                x.m.ChannelId,
                x.m.SentAt))
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request)
    {
        var result = await sender.Send(new SendMessage.Command(request.Content, request.UserId, id));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPatch("messages/{id:guid}")]
    public async Task<IActionResult> EditMessage(Guid id, [FromBody] EditMessageRequest request)
    {
        var result = await sender.Send(new EditMessage.Command(id, request.Content, request.UserId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("messages/{id:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id, [FromQuery] Guid userId)
    {
        var result = await sender.Send(new DeleteMessage.Command(id, userId));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
