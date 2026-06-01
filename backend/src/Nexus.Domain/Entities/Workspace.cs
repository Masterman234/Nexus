using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class Workspace : AggregateRoot<Guid>
{
    private Workspace(Guid id, string name, string description, Guid ownerId) : base(id)
    {
        Name = name;
        Description = description;
        OwnerId = ownerId;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Workspace Create(string name, string description, Guid ownerId)
    {
        return new Workspace(Guid.NewGuid(), name, description, ownerId);
    }
}
