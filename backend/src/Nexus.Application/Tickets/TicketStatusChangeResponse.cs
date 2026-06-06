using Nexus.Domain.Entities;

namespace Nexus.Application.Tickets;

public record TicketStatusChangeResponse(
    Guid Id,
    Guid TicketId,
    TicketStatus? OldStatus,
    TicketStatus NewStatus,
    Guid ChangedByUserId,
    DateTime CreatedAt);
