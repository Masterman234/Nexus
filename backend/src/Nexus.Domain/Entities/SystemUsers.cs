namespace Nexus.Domain.Entities;

/// <summary>
/// Well-known deterministic identifiers for system (bot) users. Stable across
/// environments so consumers can safely reference them as a FK without a lookup.
/// </summary>
public static class SystemUsers
{
    public static readonly Guid GithubBotId = new("11111111-1111-1111-1111-111111111111");
    public const string GithubBotUsername = "github-bot";
    public const string GithubBotEmail = "github-bot@nexus.system";
}
