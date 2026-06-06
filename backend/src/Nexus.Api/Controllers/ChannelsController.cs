using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Abstractions;
using Nexus.Application.Channels.Commands.SendMessage;
using Nexus.Application.Channels.Queries.GetChannels;
using Nexus.Application.Channels.Commands.EditMessage;
using Nexus.Application.Channels.Commands.DeleteMessage;
using Nexus.Application.Channels.Queries.GetMessages;
using Nexus.Api.Requests.Channels;

namespace Nexus.Api.Controllers;

[Authorize]
public class ChannelsController(ISender sender, ICurrentUser currentUser) : ApiController(sender)
{
    [HttpGet]
    public async Task<IActionResult> GetChannels([FromQuery] Guid? workspaceId)
    {
        return HandleResult(await Sender.Send(new GetChannels.Query(workspaceId)));
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id)
    {
        return HandleResult(await Sender.Send(new GetMessages.Query(id)));
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request)
    {
        var result = await Sender.Send(new SendMessage.Command(request.Content, currentUser.Id, id));
        return HandleResult(result);
    }

    [HttpPatch("messages/{id:guid}")]
    public async Task<IActionResult> EditMessage(Guid id, [FromBody] EditMessageRequest request)
    {
        var result = await Sender.Send(new EditMessage.Command(id, request.Content, currentUser.Id));
        return HandleResult(result);
    }

    [HttpDelete("messages/{id:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        var result = await Sender.Send(new DeleteMessage.Command(id, currentUser.Id));
        return HandleResult(result);
    }
}
