namespace Nexus.Application.Tickets;

public record TicketCommentResponse(
    Guid Id,
    Guid TicketId,
    Guid UserId,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);
