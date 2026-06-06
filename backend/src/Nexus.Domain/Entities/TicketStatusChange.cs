using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class TicketStatusChange : Entity<Guid>
{
    private TicketStatusChange(
        Guid id,
        Guid ticketId,
        TicketStatus? oldStatus,
        TicketStatus newStatus,
        Guid changedByUserId) : base(id)
    {
        TicketId = ticketId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid TicketId { get; private set; }
    public TicketStatus? OldStatus { get; private set; }
    public TicketStatus NewStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static TicketStatusChange Create(
        Guid ticketId,
        TicketStatus? oldStatus,
        TicketStatus newStatus,
        Guid changedByUserId)
    {
        return new TicketStatusChange(
            Guid.NewGuid(),
            ticketId,
            oldStatus,
            newStatus,
            changedByUserId);
    }
}
