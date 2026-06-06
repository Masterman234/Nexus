namespace Nexus.Application.Auth;

public record UserResponse(Guid Id, string Email, string Username, string Role);

/// <summary>
/// Returned by /login, /register, and /refresh. The refresh token is NOT in the
/// body — it ships in an HttpOnly Secure cookie set by the controller. Exposing
/// only the access token here keeps the SPA's XSS attack surface to a 15-minute
/// blast radius.
/// </summary>
/// <param name="AccessToken">Short-lived JWT for Authorization: Bearer.</param>
/// <param name="ExpiresAt">UTC instant when <paramref name="AccessToken"/> expires.</param>
/// <param name="User">Identity payload for client-side display/state.</param>
public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);
