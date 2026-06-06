namespace Nexus.Application.ChatCommands;

/// <summary>
/// Result of parsing a chat message's text for a slash command.
/// <para>
/// Tokenisation is deliberately minimal: split on whitespace, treat the first
/// token (stripped of its leading <c>/</c>) as the command name, the rest as
/// raw args joined with single spaces. Quoted arguments and flag-style parsing
/// are explicit non-features at this layer — individual command handlers can
/// re-parse <see cref="ArgsText"/> if they need more structure (e.g. EPIC-08
/// <c>/ticket new "Title with spaces"</c>).
/// </para>
/// </summary>
public sealed record ChatCommand(string Name, string ArgsText)
{
    /// <summary>Returns null if <paramref name="content"/> is not a slash command.</summary>
    public static ChatCommand? TryParse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        // Trim once so leading whitespace doesn't disqualify the command.
        var trimmed = content.TrimStart();
        if (trimmed.Length < 2 || trimmed[0] != '/') return null;

        // First whitespace separates command name from args. If there's no
        // whitespace, the entire trimmed-minus-slash string is the name.
        var afterSlash = trimmed[1..];
        var firstSpace = afterSlash.IndexOf(' ');
        if (firstSpace < 0)
        {
            return new ChatCommand(afterSlash.ToLowerInvariant(), string.Empty);
        }

        var name = afterSlash[..firstSpace].ToLowerInvariant();
        var args = afterSlash[(firstSpace + 1)..].Trim();
        return new ChatCommand(name, args);
    }
}
