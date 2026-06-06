namespace Nexus.Api.Authorization;

/// <summary>
/// Centralized policy names so controllers reference compile-checked constants
/// instead of typo-prone literals. Add new policies here as the AuthZ surface grows.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Engineer-or-Admin. Required for incident declare/resolve and engineering tooling.</summary>
    public const string RequireEngineer = nameof(RequireEngineer);

    /// <summary>Admin only. Required for changing roles and other privileged ops.</summary>
    public const string RequireAdmin = nameof(RequireAdmin);
}
