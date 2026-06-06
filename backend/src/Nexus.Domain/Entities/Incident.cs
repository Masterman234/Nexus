using Nexus.Domain.Primitives;

namespace Nexus.Domain.Entities;

public class Incident : AggregateRoot<Guid>
{
    private Incident(
        Guid id,
        string title,
        string description,
        IncidentStatus status,
        IncidentSeverity severity,
        Guid declaredByUserId,
        Guid workspaceId,
        Guid? dedicatedChannelId) : base(id)
    {
        Title = title;
        Description = description;
        Status = status;
        Severity = severity;
        DeclaredByUserId = declaredByUserId;
        WorkspaceId = workspaceId;
        DedicatedChannelId = dedicatedChannelId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public string Title { get; private set; }
    public string Description { get; private set; }
    public IncidentStatus Status { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public Guid DeclaredByUserId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    
    /// <summary>
    /// The channel specifically created for managing this incident (e.g. #inc-database-down).
    /// </summary>
    public Guid? DedicatedChannelId { get; private set; }
    
    public string? PostmortemContent { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public static Incident Create(
        string title,
        string description,
        IncidentSeverity severity,
        Guid declaredByUserId,
        Guid workspaceId,
        Guid? dedicatedChannelId)
    {
        return new Incident(
            Guid.NewGuid(),
            title,
            description,
            IncidentStatus.Investigating,
            severity,
            declaredByUserId,
            workspaceId,
            dedicatedChannelId);
    }

    public void UpdateStatus(IncidentStatus newStatus)
    {
        if (Status == newStatus) return;

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus is IncidentStatus.Resolved or IncidentStatus.Closed or IncidentStatus.PostmortemDrafted)
        {
            // Only set ResolvedAt the first time it's resolved.
            ResolvedAt ??= DateTime.UtcNow;
        }
    }

    public void SetPostmortem(string content)
    {
        PostmortemContent = content;
        Status = IncidentStatus.PostmortemDrafted;
        UpdatedAt = DateTime.UtcNow;
    }
}
