using Nexus.Domain.Entities;

namespace Nexus.Application.Tickets;

public record TicketResponse(
    Guid Id,
    int Number,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid? AssigneeUserId,
    string? AssigneeUsername,
    Guid CreatorUserId,
    Guid WorkspaceId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt);
