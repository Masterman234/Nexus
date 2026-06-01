namespace Nexus.Application.Auth.IntegrationEvents;

public record UserCreatedIntegrationEvent(Guid UserId, string Email, string Username);
