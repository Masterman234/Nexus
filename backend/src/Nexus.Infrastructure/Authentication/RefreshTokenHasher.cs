using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nexus.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Nexus.Infrastructure.Authentication;

public class RefreshTokenHasher(IOptions<JwtOptions> options) : IRefreshTokenHasher
{
    // 32 bytes = 256 bits of entropy. Sufficient that a brute-force attacker can't
    // guess a valid token within its lifetime even with the hash algorithm known.
    private const int RawTokenBytes = 32;

    private readonly byte[] _pepper = Encoding.UTF8.GetBytes(options.Value.RefreshTokenPepper);

    public string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RawTokenBytes);
        // Base64Url is URL- and cookie-safe (no '+'/'/'/'=' padding to escape).
        return Base64UrlEncoder.Encode(bytes);
    }

    public string Hash(string rawToken)
    {
        // HMAC mixes the pepper into the digest — equivalent to "salted hash" but
        // with a global server-side key instead of per-row. A DB-only leak yields
        // hashes the attacker cannot reverse or replay without also stealing the
        // pepper from app config.
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        using var hmac = new HMACSHA256(_pepper);
        var digest = hmac.ComputeHash(bytes);
        return Convert.ToHexString(digest);
    }
}
