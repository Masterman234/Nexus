namespace Nexus.Application.Tickets.IntegrationEvents;

public record TicketAssignedIntegrationEvent(
    Guid TicketId,
    Guid? AssigneeUserId,
    DateTime UpdatedAt);
