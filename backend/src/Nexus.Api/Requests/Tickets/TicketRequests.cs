using Nexus.Domain.Entities;

namespace Nexus.Api.Requests.Tickets;

public record CreateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    Guid WorkspaceId);

public record UpdateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority);

public record TransitionStatusRequest(TicketStatus NewStatus);

public record AssignTicketRequest(Guid? AssigneeUserId);

public record AddCommentRequest(string Content);
