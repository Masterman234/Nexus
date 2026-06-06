namespace Nexus.Infrastructure.Authentication;

/// <summary>
/// Strongly-typed binding for the "Jwt" configuration section. All values are
/// required at startup — DI throws if any are missing so a misconfigured deploy
/// fails fast instead of silently issuing tokens nobody can validate.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC signing key for access tokens. Must be ≥32 bytes for HS256.</summary>
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Nexus";
    public string Audience { get; set; } = "Nexus";

    /// <summary>
    /// Access token lifetime. Short on purpose: stolen access tokens can't be
    /// revoked, so we rely on the refresh-token rotation flow to limit blast radius.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Refresh token lifetime; rotated on every successful refresh.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Server-side pepper mixed into HMAC-SHA256 when hashing refresh tokens at rest.
    /// A DB-only leak (without app secrets) yields unusable hashes; a full app-host
    /// compromise gives up everything anyway. Rotate by issuing all-new tokens.
    /// </summary>
    public string RefreshTokenPepper { get; set; } = string.Empty;
}
