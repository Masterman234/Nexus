using System.Text.RegularExpressions;
using Nexus.Application.Abstractions;

namespace Nexus.Application.Services;

public class ReferenceExtractor : IReferenceExtractor
{
    // Regex for Tickets (NEX-123)
    private static readonly Regex TicketRegex = new(@"\bNEX-(\d+)\b", RegexOptions.IgnoreCase);
    
    // Regex for GitHub PRs (#456) - simplified to avoid matching #hashtags
    private static readonly Regex PullRequestRegex = new(@"(?<!\w)#(\d+)\b", RegexOptions.IgnoreCase);
    
    // Regex for Incidents (SEV-1)
    private static readonly Regex IncidentRegex = new(@"\bSEV-(\d+)\b", RegexOptions.IgnoreCase);

    public IEnumerable<ExtractedReference> Extract(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Enumerable.Empty<ExtractedReference>();

        var tickets = TicketRegex.Matches(text)
            .Cast<Match>()
            .Select(m => new ExtractedReference("Ticket", m.Value.ToUpperInvariant()));

        var prs = PullRequestRegex.Matches(text)
            .Cast<Match>()
            .Select(m => new ExtractedReference("PullRequest", m.Value));

        var incidents = IncidentRegex.Matches(text)
            .Cast<Match>()
            .Select(m => new ExtractedReference("Incident", m.Value.ToUpperInvariant()));

        // Combine all and return unique values based on the reference string
        return tickets.Concat(prs).Concat(incidents)
            .GroupBy(r => r.Value)
            .Select(g => g.First());
    }
}
