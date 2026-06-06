using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Abstractions;
using Nexus.Application.Tickets.Commands.CreateTicket;
using Nexus.Application.Tickets.Commands.TransitionTicketStatus;
using Nexus.Application.Tickets.Commands.AssignTicket;
using Nexus.Application.Tickets.Commands.AddTicketComment;
using Nexus.Application.Tickets.Queries.ListTickets;
using Nexus.Application.Tickets.Queries.GetTicketById;
using Nexus.Application.Tickets.Commands.UpdateTicket;
using Nexus.Domain.Entities;
using Nexus.Api.Requests.Tickets;

namespace Nexus.Api.Controllers;

[Authorize]
public class TicketsController(ISender sender, ICurrentUser currentUser) : ApiController(sender)
{
    [HttpGet]
    public async Task<IActionResult> ListTickets(
        [FromQuery] Guid workspaceId,
        [FromQuery] Guid? assigneeUserId,
        [FromQuery] TicketStatus? status)
    {
        return HandleResult(await Sender.Send(new ListTickets.Query(workspaceId, assigneeUserId, status)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        return HandleResult(await Sender.Send(new GetTicketById.Query(id)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var result = await Sender.Send(new CreateTicket.Command(
            request.Title,
            request.Description,
            request.Priority,
            currentUser.Id,
            request.WorkspaceId));

        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> TransitionStatus(Guid id, [FromBody] TransitionStatusRequest request)
    {
        var result = await Sender.Send(new TransitionTicketStatus.Command(id, request.NewStatus, currentUser.Id));
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] UpdateTicketRequest request)
    {
        var result = await Sender.Send(new UpdateTicket.Command(
            id,
            request.Title,
            request.Description,
            request.Priority));

        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/assign")]
    public async Task<IActionResult> AssignTicket(Guid id, [FromBody] AssignTicketRequest request)
    {
        var result = await Sender.Send(new AssignTicket.Command(id, request.AssigneeUserId));
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request)
    {
        var result = await Sender.Send(new AddTicketComment.Command(id, currentUser.Id, request.Content));
        return HandleResult(result);
    }
}
