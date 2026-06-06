using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class Ticket : AggregateRoot<Guid>
{
    private Ticket(
        Guid id,
        int number,
        string title,
        string description,
        TicketStatus status,
        TicketPriority priority,
        Guid creatorUserId,
        Guid workspaceId) : base(id)
    {
        Number = number;
        Title = title;
        Description = description;
        Status = status;
        Priority = priority;
        CreatorUserId = creatorUserId;
        WorkspaceId = workspaceId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public int Number { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public Guid? AssigneeUserId { get; private set; }
    public Guid CreatorUserId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public static Ticket Create(
        int number,
        string title,
        string description,
        TicketStatus status,
        TicketPriority priority,
        Guid creatorUserId,
        Guid workspaceId)
    {
        return new Ticket(
            Guid.NewGuid(),
            number,
            title,
            description,
            status,
            priority,
            creatorUserId,
            workspaceId);
    }

    public void Update(string title, string description, TicketPriority priority)
    {
        Title = title;
        Description = description;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(TicketStatus newStatus)
    {
        if (Status == newStatus) return;

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus is TicketStatus.Done or TicketStatus.Closed)
        {
            ResolvedAt = DateTime.UtcNow;
        }
        else
        {
            ResolvedAt = null;
        }
    }

    public void Assign(Guid? userId)
    {
        AssigneeUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}
