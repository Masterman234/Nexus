using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class Channel : AggregateRoot<Guid>
{
    private Channel(Guid id, string name, string description, Guid workspaceId) : base(id)
    {
        Name = name;
        Description = description;
        WorkspaceId = workspaceId;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Channel Create(string name, string description, Guid workspaceId)
    {
        return new Channel(Guid.NewGuid(), name, description, workspaceId);
    }
}
