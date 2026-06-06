using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class TicketComment : Entity<Guid>
{
    private TicketComment(
        Guid id,
        Guid ticketId,
        Guid userId,
        string content) : base(id)
    {
        TicketId = ticketId;
        UserId = userId;
        Content = content;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid TicketId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static TicketComment Create(Guid ticketId, Guid userId, string content)
    {
        return new TicketComment(Guid.NewGuid(), ticketId, userId, content);
    }

    public void UpdateContent(string newContent)
    {
        Content = newContent;
        UpdatedAt = DateTime.UtcNow;
    }
}
