using Nexus.Domain.Entities;

namespace Nexus.Application.Abstractions;

public interface IJwtProvider
{
    string Generate(User user);
}
