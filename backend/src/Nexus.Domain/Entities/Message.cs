using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class Message : AggregateRoot<Guid>
{
    private Message(Guid id, string content, Guid userId, Guid channelId) : base(id)
    {
        Content = content;
        UserId = userId;
        ChannelId = channelId;
        SentAt = DateTime.UtcNow;
    }

    public string Content { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ChannelId { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Message Create(string content, Guid userId, Guid channelId)
    {
        return new Message(Guid.NewGuid(), content, userId, channelId);
    }

    public void UpdateContent(string newContent)
    {
        if (string.IsNullOrWhiteSpace(newContent)) return;
        Content = newContent;
        UpdatedAt = DateTime.UtcNow;
    }
}
