using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class ExternalEvent : AggregateRoot<Guid>
{
    private ExternalEvent(Guid id, string source, string eventType, string payload) : base(id)
    {
        Source = source;
        EventType = eventType;
        Payload = payload;
        ReceivedAt = DateTime.UtcNow;
    }

    public string Source { get; private set; } // e.g., "GitHub", "GitLab"
    public string EventType { get; private set; } // e.g., "push", "pull_request"
    public string Payload { get; private set; } // Raw JSON
    public DateTime ReceivedAt { get; private set; }

    public static ExternalEvent Create(string source, string eventType, string payload)
    {
        return new ExternalEvent(Guid.NewGuid(), source, eventType, payload);
    }
}
