using Nexus.Domain.Entities;

namespace Nexus.Application.Tickets.IntegrationEvents;

public record TicketStatusChangedIntegrationEvent(
    Guid TicketId,
    TicketStatus OldStatus,
    TicketStatus NewStatus,
    Guid ChangedByUserId,
    DateTime UpdatedAt);
