using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

/// <summary>
/// Represents an implicit link between entities discovered in text (e.g., "Closes NEX-123" in a PR).
/// This is the backbone of the "Smart Cross-Linking" feature.
/// </summary>
public class EntityReference : Entity<Guid>
{
    private EntityReference(
        Guid id,
        Guid sourceEntityId,
        string sourceEntityType,
        string targetEntityType,
        string targetValue,
        Guid? targetEntityId = null) : base(id)
    {
        SourceEntityId = sourceEntityId;
        SourceEntityType = sourceEntityType;
        TargetEntityType = targetEntityType;
        TargetValue = targetValue;
        TargetEntityId = targetEntityId;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid SourceEntityId { get; private set; }
    public string SourceEntityType { get; private set; } // "Message", "Commit", "PullRequest", "Ticket"
    public string TargetEntityType { get; private set; } // "Ticket", "PullRequest", "Incident"
    public string TargetValue { get; private set; }     // "NEX-123", "#456", "SEV-1"
    public Guid? TargetEntityId { get; private set; }   // Resolved ID if the target exists in our DB
    public DateTime CreatedAt { get; private set; }

    public static EntityReference Create(
        Guid sourceEntityId,
        string sourceEntityType,
        string targetEntityType,
        string targetValue,
        Guid? targetEntityId = null)
    {
        return new EntityReference(
            Guid.NewGuid(),
            sourceEntityId,
            sourceEntityType,
            targetEntityType,
            targetValue,
            targetEntityId);
    }
}
