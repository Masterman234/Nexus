using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexus.Application.Abstractions;
using Nexus.Application.Engineering.IntegrationEvents;
using Nexus.Application.Tickets.Commands.TransitionTicketStatus;
using Nexus.Domain.Entities;
using System.Text.RegularExpressions;

namespace Nexus.Application.Tickets.Consumers;

public class PullRequestMergedConsumer(
    IApplicationDbContext dbContext,
    ISender mediator,
    ILogger<PullRequestMergedConsumer> logger) 
    : IConsumer<PullRequestMergedIntegrationEvent>
{
    private static readonly Regex TicketRegex = new(@"\b(Close|Closes|Closed|Fix|Fixes|Fixed|Resolve|Resolves|Resolved)\s+NEX-(\d+)\b", RegexOptions.IgnoreCase);

    public async Task Consume(ConsumeContext<PullRequestMergedIntegrationEvent> context)
    {
        var @event = context.Message;
        
        var pr = await dbContext.PullRequests
            .FirstOrDefaultAsync(p => p.Id == @event.PullRequestId, context.CancellationToken);

        if (pr is null)
        {
            logger.LogWarning(">>> [TICKET] PR {PullRequestId} not found for auto-transition.", @event.PullRequestId);
            return;
        }

        if (string.IsNullOrWhiteSpace(pr.Description)) return;

        var matches = TicketRegex.Matches(pr.Description);
        if (matches.Count == 0) return;

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[2].Value, out var ticketNumber))
            {
                logger.LogInformation(">>> [TICKET] Auto-transitioning NEX-{TicketNumber} due to PR {PrNumber} merge.", 
                    ticketNumber, pr.Number);

                // For this prototype, we transition ALL tickets with this number across all workspaces.
                // In a production multi-tenant system, we would narrow by WorkspaceId linked to the Repository.
                var tickets = await dbContext.Tickets
                    .Where(t => t.Number == ticketNumber && t.Status != TicketStatus.Done && t.Status != TicketStatus.Closed)
                    .ToListAsync(context.CancellationToken);

                foreach (var ticket in tickets)
                {
                    await mediator.Send(new TransitionTicketStatus.Command(
                        ticket.Id, 
                        TicketStatus.Done, 
                        SystemUsers.GithubBotId), context.CancellationToken);
                }
            }
        }
    }
}
