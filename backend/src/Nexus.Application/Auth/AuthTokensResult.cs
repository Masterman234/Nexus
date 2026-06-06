namespace Nexus.Application.Auth;

/// <summary>
/// Internal handler output bundling the API response with the raw refresh token.
/// The controller pulls <see cref="RefreshToken"/> off and emits it as an HttpOnly
/// cookie; only <see cref="Response"/> ever reaches the JSON body. Kept in the
/// Application layer so handlers don't need to know about HTTP cookies.
/// </summary>
public record AuthTokensResult(AuthResponse Response, string RefreshToken, DateTime RefreshTokenExpiresAt);
