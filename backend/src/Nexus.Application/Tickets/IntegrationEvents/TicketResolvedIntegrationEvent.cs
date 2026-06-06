namespace Nexus.Application.Tickets.IntegrationEvents;

public record TicketResolvedIntegrationEvent(
    Guid TicketId,
    DateTime ResolvedAt);
