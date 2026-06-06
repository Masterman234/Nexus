using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Nexus.SharedKernel;
using System.Text;

namespace Nexus.Application.Engineering.Commands.ResolveIncident;

public static class ResolveIncident
{
    public record Command(Guid ChannelId, Guid UserId) : IRequest<Result<ResolveIncidentResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChannelId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public class Handler(IApplicationDbContext dbContext, IAIService aiService)
        : IRequestHandler<Command, Result<ResolveIncidentResponse>>
    {
        public async Task<Result<ResolveIncidentResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            // 1. Find the incident associated with this war room channel
            var incident = await dbContext.Incidents
                .FirstOrDefaultAsync(i => i.DedicatedChannelId == request.ChannelId, cancellationToken);

            if (incident is null)
            {
                return Result<ResolveIncidentResponse>.Failure("This channel is not an active incident war room.");
            }

            if (incident.Status == IncidentStatus.Resolved || incident.Status == IncidentStatus.Closed || incident.Status == IncidentStatus.PostmortemDrafted)
            {
                return Result<ResolveIncidentResponse>.Failure("This incident has already been resolved.");
            }

            // 2. Fetch the chat history for the war room
            var messages = await dbContext.Messages
                .Where(m => m.ChannelId == request.ChannelId)
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);

            // Need to join user display names for better context
            var userIds = messages.Select(m => m.UserId).Distinct().ToList();
            var users = await dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

            // 3. Compile the history into a context string
            var historyBuilder = new StringBuilder();
            foreach (var msg in messages)
            {
                var username = users.TryGetValue(msg.UserId, out var uname) ? uname : "UnknownUser";
                historyBuilder.AppendLine($"[{msg.SentAt:HH:mm:ss}] {username}: {msg.Content}");
            }

            var chatLog = historyBuilder.ToString();

            // 4. Generate the AI Postmortem
            var prompt = $@"
You are an expert Site Reliability Engineer (SRE). 
An incident titled '{incident.Title}' with severity '{incident.Severity}' has just been resolved.
Below is the raw chat log from the incident's dedicated war room channel where engineers diagnosed and fixed the issue.

Please write a professional, concise postmortem report using the following structure:
## Executive Summary
(1-2 sentences explaining what happened and impact)

## Root Cause
(What was the actual technical underlying issue based on the chat)

## Resolution
(What steps were taken to fix the issue)

## Timeline
(Bullet points of 3-5 key events with timestamps from the chat log)

WAR ROOM CHAT LOG:
{chatLog}
";

            var aiResult = await aiService.PromptAsync(prompt, cancellationToken);

            if (aiResult.IsFailure)
            {
                return Result<ResolveIncidentResponse>.Failure($"Failed to generate postmortem: {aiResult.Error}");
            }

            // 5. Update the Incident
            incident.UpdateStatus(IncidentStatus.Resolved);
            incident.SetPostmortem(aiResult.Value);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<ResolveIncidentResponse>.Success(new ResolveIncidentResponse(
                incident.Id,
                incident.Title,
                incident.Status,
                incident.PostmortemContent));
        }
    }
}

public record ResolveIncidentResponse(
    Guid Id,
    string Title,
    IncidentStatus Status,
    string? PostmortemContent);
