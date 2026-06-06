using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nexus.Infrastructure.Authentication;

public class JwtProvider(IOptions<JwtOptions> options) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;

    public TimeSpan RefreshTokenLifetime => _options.RefreshTokenLifetime;

    public (string Token, DateTime ExpiresAt) Generate(User user)
    {
        // Role claim flows through ASP.NET's [Authorize(Roles = ...)] and policy
        // handlers (RolesAuthorizationRequirement reads ClaimTypes.Role).
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.Add(_options.AccessTokenLifetime);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
