namespace Nexus.Application.Abstractions;

public interface ICurrentUser
{
    Guid Id { get; }
    string? Username { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
