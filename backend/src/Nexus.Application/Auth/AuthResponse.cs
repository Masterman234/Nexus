namespace Nexus.Application.Auth;

public record UserResponse(Guid Id, string Email, string Username);

public record AuthResponse(string Token, UserResponse User);
