using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.Application.Abstractions;
using Nexus.Application.Channels;
using Nexus.Application.Engineering.Commands.GenerateStandup;
using Nexus.Domain.Entities;

namespace Nexus.Application.ChatCommands;

/// <summary>
/// Default router. Public surface is <see cref="Schedule"/> — the actual work
/// runs in a new DI scope via <see cref="IServiceScopeFactory"/> so it can
/// outlive the HTTP request that triggered it. The dispatch table is a plain
/// switch on <see cref="ChatCommand.Name"/> for now — small and obvious. When
/// the command list grows past ~6 entries we'll lift it to a registry of
/// <c>IChatCommandHandler</c> implementations, but a switch is the cheapest
/// thing that works while we're discovering the right surface area.
/// </summary>
public sealed class ChatCommandRouter(
    IServiceScopeFactory scopeFactory,
    ILogger<ChatCommandRouter> logger) : IChatCommandRouter
{
    private const string HelpText =
        "Available commands:\n" +
        "- `/standup` — AI summary of your activity in the last 48 hours\n" +
        "- `/incident declare \"Title\" Sev1` — Declare a new incident\n" +
        "- `/ticket new \"Title\"` — Create a new ticket\n" +
        "- `/ticket list` — List open tickets in this workspace\n" +
        "- `/ticket NEX-12` — Show ticket details\n" +
        "- `/ticket assign NEX-12 @user` — Assign a ticket\n" +
        "- `/ticket close NEX-12` — Close a ticket\n" +
        "- `/ticket comment NEX-12 \"...\"` — Add a comment\n" +
        "- `/help` — show this list";

    public void Schedule(ChatCommand command, Guid invokingUserId, Guid channelId)
    {
        // ... (existing Schedule logic)
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteAsync(command, invokingUserId, channelId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ">>> [CMD] Background dispatch crashed for '{Name}': {Message}",
                    command.Name, ex.Message);
            }
        });
    }

    private async Task ExecuteAsync(
        ChatCommand command, Guid invokingUserId, Guid channelId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

        logger.LogInformation(">>> [CMD] Dispatch '{Name}' invoker={UserId} channel={ChannelId}",
            command.Name, invokingUserId, channelId);

        string reply;
        try
        {
            reply = command.Name switch
            {
                "standup" => await HandleStandupAsync(mediator, invokingUserId, ct),
                "incident" => await HandleIncidentAsync(mediator, dbContext, invokingUserId, channelId, command.ArgsText, ct),
                "ticket" => await HandleTicketAsync(mediator, dbContext, invokingUserId, channelId, command.ArgsText, ct),
                "help" => HelpText,
                _ => $"Unknown command `/{command.Name}`. Type `/help` to see what's available.",
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ">>> [CMD] Handler threw for '{Name}': {Message}", command.Name, ex.Message);
            reply = $"⚠️ Command `/{command.Name}` failed: {ex.Message}";
        }

        await PostBotReplyAsync(dbContext, chatService, reply, channelId, ct);
    }

    private async Task<string> HandleIncidentAsync(
        ISender mediator, IApplicationDbContext dbContext,
        Guid userId, Guid channelId, string argsText, CancellationToken ct)
    {
        var channel = await dbContext.Channels.FindAsync(new object[] { channelId }, ct);
        if (channel is null) return "⚠️ Internal error: Channel not found.";

        var workspaceId = channel.WorkspaceId;
        var args = argsText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0) return "Usage: `/incident declare \"Title\" Sev1` or `/incident resolve`";

        var subCommand = args[0].ToLowerInvariant();

        if (subCommand == "declare")
        {
            var firstQuote = argsText.IndexOf('"');
            var lastQuote = argsText.LastIndexOf('"');
            
            if (firstQuote < 0 || lastQuote <= firstQuote) 
                return "Usage: `/incident declare \"Title\" Sev1`";

            var title = argsText.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
            var rest = argsText.Substring(lastQuote + 1).Trim();
            
            if (!Enum.TryParse<IncidentSeverity>(rest, true, out var severity))
            {
                severity = IncidentSeverity.Sev2; // Default if not provided or invalid
            }

            var result = await mediator.Send(new Engineering.Commands.DeclareIncident.DeclareIncident.Command(
                title, "", severity, userId, workspaceId), ct);

            return result.IsSuccess 
                ? $"🚨 **INCIDENT DECLARED** 🚨\nSeverity: `{result.Value.Severity}` | Title: **{result.Value.Title}**\nA dedicated war room channel has been created."
                : $"⚠️ Failed to declare incident: {result.Error}";
        }
        else if (subCommand == "resolve")
        {
            var result = await mediator.Send(new Engineering.Commands.ResolveIncident.ResolveIncident.Command(channelId, userId), ct);
            
            return result.IsSuccess
                ? $"✅ **INCIDENT RESOLVED** ✅\n\n**AI Generated Postmortem:**\n\n{result.Value.PostmortemContent}"
                : $"⚠️ Failed to resolve incident: {result.Error}";
        }

        return $"Unknown subcommand `{subCommand}`. Type `/help` to see what's available.";
    }

    private async Task<string> HandleTicketAsync(
        ISender mediator, IApplicationDbContext dbContext,
        Guid userId, Guid channelId, string argsText, CancellationToken ct)
    {
        var channel = await dbContext.Channels.FindAsync(new object[] { channelId }, ct);
        if (channel is null) return "⚠️ Internal error: Channel not found.";

        var workspaceId = channel.WorkspaceId;
        var args = argsText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0) return "Usage: `/ticket [new|assign|close|comment|list|NEX-12]`";

        var subCommand = args[0].ToLowerInvariant();

        return subCommand switch
        {
            "new" => await HandleTicketNewAsync(mediator, userId, workspaceId, argsText, ct),
            "assign" => await HandleTicketAssignAsync(mediator, dbContext, workspaceId, argsText, ct),
            "close" => await HandleTicketCloseAsync(mediator, userId, workspaceId, argsText, ct),
            "comment" => await HandleTicketCommentAsync(mediator, userId, workspaceId, argsText, ct),
            "list" => await HandleTicketListAsync(mediator, workspaceId, ct),
            _ when subCommand.StartsWith("nex-") => await HandleTicketShowAsync(mediator, workspaceId, subCommand, ct),
            _ => $"Unknown subcommand `{subCommand}`. Type `/help` to see what's available."
        };
    }

    private static async Task<string> HandleTicketNewAsync(ISender mediator, Guid userId, Guid workspaceId, string argsText, CancellationToken ct)
    {
        var title = argsText.Substring(argsText.IndexOf("new") + 3).Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(title)) return "Usage: `/ticket new \"Title\"`";

        var result = await mediator.Send(new Tickets.Commands.CreateTicket.CreateTicket.Command(
            title, "", TicketPriority.Medium, userId, workspaceId), ct);

        return result.IsSuccess 
            ? $"✅ Created Ticket **NEX-{result.Value.Number}**: {result.Value.Title}" 
            : $"⚠️ Failed: {result.Error}";
    }

    private static async Task<string> HandleTicketShowAsync(ISender mediator, Guid workspaceId, string subCommand, CancellationToken ct)
    {
        if (!int.TryParse(subCommand.Replace("nex-", ""), out var number))
            return "⚠️ Invalid ticket number format. Use `NEX-123`.";

        var result = await mediator.Send(new Tickets.Queries.GetTicketByNumber.GetTicketByNumber.Query(workspaceId, number), ct);
        if (result.IsFailure) return $"⚠️ {result.Error}";

        var t = result.Value.Ticket;
        return $"🎫 **NEX-{t.Number}: {t.Title}**\n" +
               $"Status: `{t.Status}` | Priority: `{t.Priority}`\n" +
               $"{t.Description}\n\n" +
               $"_Comments: {result.Value.Comments.Count} | History: {result.Value.History.Count}_";
    }

    private static async Task<string> HandleTicketListAsync(ISender mediator, Guid workspaceId, CancellationToken ct)
    {
        var result = await mediator.Send(new Tickets.Queries.ListTickets.ListTickets.Query(workspaceId, Status: TicketStatus.Open), ct);
        if (result.IsFailure) return $"⚠️ {result.Error}";

        if (result.Value.Count == 0) return "No open tickets found in this workspace.";

        var list = string.Join("\n", result.Value.Take(10).Select(t => $"- **NEX-{t.Number}**: {t.Title} (`{t.Status}`)"));
        if (result.Value.Count > 10) list += $"\n_... and {result.Value.Count - 10} more_";

        return $"📋 **Open Tickets**\n\n{list}";
    }

    private static async Task<string> HandleTicketAssignAsync(ISender mediator, IApplicationDbContext dbContext, Guid workspaceId, string argsText, CancellationToken ct)
    {
        // /ticket assign NEX-12 @user
        var parts = argsText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return "Usage: `/ticket assign NEX-12 @username`";

        if (!int.TryParse(parts[1].ToLowerInvariant().Replace("nex-", ""), out var number))
            return "⚠️ Invalid ticket number.";

        var username = parts[2].TrimStart('@');
        var assignee = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (assignee is null) return $"⚠️ User `{username}` not found.";

        var ticketResult = await mediator.Send(new Tickets.Queries.GetTicketByNumber.GetTicketByNumber.Query(workspaceId, number), ct);
        if (ticketResult.IsFailure) return $"⚠️ {ticketResult.Error}";

        var result = await mediator.Send(new Tickets.Commands.AssignTicket.AssignTicket.Command(ticketResult.Value.Ticket.Id, assignee.Id), ct);
        return result.IsSuccess ? $"✅ Assigned **NEX-{number}** to @{username}" : $"⚠️ {result.Error}";
    }

    private static async Task<string> HandleTicketCloseAsync(ISender mediator, Guid userId, Guid workspaceId, string argsText, CancellationToken ct)
    {
        var parts = argsText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return "Usage: `/ticket close NEX-12`";

        if (!int.TryParse(parts[1].ToLowerInvariant().Replace("nex-", ""), out var number))
            return "⚠️ Invalid ticket number.";

        var ticketResult = await mediator.Send(new Tickets.Queries.GetTicketByNumber.GetTicketByNumber.Query(workspaceId, number), ct);
        if (ticketResult.IsFailure) return $"⚠️ {ticketResult.Error}";

        var result = await mediator.Send(new Tickets.Commands.TransitionTicketStatus.TransitionTicketStatus.Command(ticketResult.Value.Ticket.Id, TicketStatus.Closed, userId), ct);
        return result.IsSuccess ? $"✅ Closed **NEX-{number}**" : $"⚠️ {result.Error}";
    }

    private static async Task<string> HandleTicketCommentAsync(ISender mediator, Guid userId, Guid workspaceId, string argsText, CancellationToken ct)
    {
        // /ticket comment NEX-12 "My comment"
        var firstQuote = argsText.IndexOf('"');
        if (firstQuote < 0) return "Usage: `/ticket comment NEX-12 \"comment text\"`";

        var ticketPart = argsText.Substring(0, firstQuote).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (ticketPart.Length < 2 || !int.TryParse(ticketPart[1].ToLowerInvariant().Replace("nex-", ""), out var number))
            return "⚠️ Invalid ticket number.";

        var content = argsText.Substring(firstQuote).Trim('"');
        
        var ticketResult = await mediator.Send(new Tickets.Queries.GetTicketByNumber.GetTicketByNumber.Query(workspaceId, number), ct);
        if (ticketResult.IsFailure) return $"⚠️ {ticketResult.Error}";

        var result = await mediator.Send(new Tickets.Commands.AddTicketComment.AddTicketComment.Command(ticketResult.Value.Ticket.Id, userId, content), ct);
        return result.IsSuccess ? $"✅ Comment added to **NEX-{number}**" : $"⚠️ {result.Error}";
    }

    private static async Task<string> HandleStandupAsync(ISender mediator, Guid invokingUserId, CancellationToken ct)
    {
        // Reuse the existing GenerateStandup handler — same code path as the
        // POST /api/v1/engineering/standup endpoint, just a different invocation
        // surface. This is the architectural payoff: one handler, two front doors.
        var result = await mediator.Send(new GenerateStandup.Command(UserId: invokingUserId), ct);

        if (result.IsFailure)
        {
            return $"⚠️ Could not generate standup: {result.Error}";
        }

        return $"📋 **Standup for the last 48 hours**\n\n{result.Value}";
    }

    private async Task PostBotReplyAsync(
        IApplicationDbContext dbContext, IChatService chatService,
        string content, Guid channelId, CancellationToken ct)
    {
        // Persist as a real Message so chat history survives a reload — same rule
        // GithubWebhookConsumer follows. Attribute to nexus-bot (not github-bot) so
        // the UI can render AI replies with a different colour / icon if it wants.
        var message = Message.Create(content, SystemUsers.NexusBotId, channelId);
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(ct);

        await chatService.BroadcastMessageAsync(channelId, new MessageResponse(
            message.Id,
            message.Content,
            SystemUsers.NexusBotUsername,
            message.ChannelId,
            message.SentAt), ct);

        logger.LogInformation(">>> [CMD] Bot reply posted to channel={ChannelId} id={MessageId}",
            channelId, message.Id);
    }
}
