using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Auth;
using Nexus.Application.Auth.Commands.GuestLogin;
using Nexus.Application.Auth.Commands.Login;
using Nexus.Application.Auth.Commands.Logout;
using Nexus.Application.Auth.Commands.Refresh;
using Nexus.Application.Auth.Commands.Register;
using Nexus.SharedKernel;
using Nexus.Api.Requests.Auth;

namespace Nexus.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[AllowAnonymous]
public class AuthController(ISender sender) : ControllerBase
{
    // Cookie name kept short and non-descriptive — clients only need to know it exists.
    // Path scoped to /api/.../auth so the cookie isn't sent on every API call (only
    // refresh + logout need it; the access token in Authorization handles everything else).
    private const string RefreshCookieName = "nexus_rt";

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await sender.Send(new RegisterUser.Command(
            request.Email,
            request.Username,
            request.Password,
            CreatedByIp: GetClientIp(),
            UserAgent: GetUserAgent()));

        return EmitTokens(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await sender.Send(new LoginUser.Command(
            request.Email,
            request.Password,
            CreatedByIp: GetClientIp(),
            UserAgent: GetUserAgent()));

        return EmitTokens(result);
    }

    [HttpPost("guest")]
    public async Task<IActionResult> Guest()
    {
        // One-click demo login — no body, no credentials. Reuses the same token-emit
        // path as login/register so the refresh cookie + response shape are identical.
        var result = await sender.Send(new GuestLogin.Command(
            CreatedByIp: GetClientIp(),
            UserAgent: GetUserAgent()));

        return EmitTokens(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        // Read the raw token from the HttpOnly cookie — never the body. The body
        // path would let an XSS-injected fetch() echo a stolen token from
        // document.cookie (which it can't read for HttpOnly cookies anyway).
        var raw = Request.Cookies[RefreshCookieName];

        var result = await sender.Send(new RefreshToken.Command(
            raw ?? string.Empty,
            CreatedByIp: GetClientIp(),
            UserAgent: GetUserAgent()));

        return EmitTokens(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var raw = Request.Cookies[RefreshCookieName];
        await sender.Send(new LogoutUser.Command(raw));

        // Clear cookie regardless of result. Use the same path/security flags so
        // the browser actually overwrites it (mismatched flags = ghost cookie).
        Response.Cookies.Delete(RefreshCookieName, BuildCookieOptions(expires: DateTimeOffset.UnixEpoch));
        return NoContent();
    }

    private IActionResult EmitTokens(Result<AuthTokensResult> result)
    {
        if (!result.IsSuccess)
        {
            // 401 for credential failures reads better than 400 to API clients.
            return Unauthorized(new { message = result.Error });
        }

        var tokens = result.Value!;
        Response.Cookies.Append(
            RefreshCookieName,
            tokens.RefreshToken,
            BuildCookieOptions(expires: tokens.RefreshTokenExpiresAt));

        return Ok(tokens.Response);
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,             // JS cannot read → XSS cannot exfiltrate
        Secure = true,               // HTTPS only; OK in dev because we use the local cert
        SameSite = SameSiteMode.Strict,
        Path = "/api",               // sent to /api/* (refresh + logout); not to /chatHub etc.
        Expires = expires,
        IsEssential = true,          // not subject to non-essential cookie consent
    };

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent()
    {
        var ua = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua[..Math.Min(ua.Length, 512)];
    }
}
