namespace Nexus.Domain.Entities;

/// <summary>
/// Coarse-grained roles for authorization. Kept intentionally small — finer access
/// rules (e.g. "ticket assignee can transition status") belong in policy handlers,
/// not new role values. If you find yourself wanting a 4th role, ask whether it's
/// really a permission or a workspace-scoped membership instead.
/// </summary>
public enum UserRole
{
    /// <summary>Default for all newly-registered users.</summary>
    Member = 0,

    /// <summary>Can declare/resolve incidents and access engineering tooling.</summary>
    Engineer = 1,

    /// <summary>System administration — can change other users' roles.</summary>
    Admin = 2,
}
