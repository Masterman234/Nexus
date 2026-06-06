namespace Nexus.Application.Abstractions;

/// <summary>
/// Hashes refresh tokens for at-rest storage. The raw token is high-entropy random
/// bytes (256-bit) so we don't need a slow KDF (BCrypt/Argon2) or a per-row salt —
/// a single HMAC pass with a server-side pepper is sufficient to defeat a
/// database-only leak while keeping verify ~1µs.
/// </summary>
public interface IRefreshTokenHasher
{
    /// <summary>Generates a cryptographically random raw token (URL-safe base64).</summary>
    string GenerateRawToken();

    /// <summary>HMAC-SHA256(rawToken, pepper). Hex-encoded for storage.</summary>
    string Hash(string rawToken);
}
