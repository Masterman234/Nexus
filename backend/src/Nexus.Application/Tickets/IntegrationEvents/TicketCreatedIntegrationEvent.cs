using Nexus.Domain.Entities;

namespace Nexus.Application.Tickets.IntegrationEvents;

public record TicketCreatedIntegrationEvent(
    Guid TicketId,
    int Number,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid CreatorUserId,
    Guid WorkspaceId,
    DateTime CreatedAt);
