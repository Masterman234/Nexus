using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure;

/// <summary>
/// Applies pending EF migrations and seeds well-known rows (system bot users)
/// that the rest of the application assumes exist. Idempotent — safe to invoke
/// on every startup.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Only run migrations if we're using a relational provider that isn't SQLite.
        // Integration tests use SQLite where we use EnsureCreated() instead.
        if (db.Database.IsNpgsql())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        await SeedSystemUsersAsync(db, cancellationToken);

        // Public-demo content (workspace, channels, messages, tickets) is opt-in via
        // config so a normal dev/prod DB stays clean. Enable with Seed:Demo=true.
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (config.GetValue("Seed:Demo", false))
        {
            await SeedDemoDataAsync(db, cancellationToken);
        }
    }

    private static async Task SeedSystemUsersAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        // Idempotent: each bot is checked + added independently so adding a new bot
        // in code doesn't require dropping the existing seed.
        await EnsureBotAsync(db, SystemUsers.GithubBotId, SystemUsers.GithubBotEmail,
            SystemUsers.GithubBotUsername, cancellationToken);
        await EnsureBotAsync(db, SystemUsers.NexusBotId, SystemUsers.NexusBotEmail,
            SystemUsers.NexusBotUsername, cancellationToken);
        // The guest is a system (passwordless) user; it authenticates only through the
        // dedicated guest-login command. Seeding it unconditionally is harmless because
        // it cannot log in via the normal flow and is excluded from the Admin bootstrap.
        await EnsureBotAsync(db, SystemUsers.GuestUserId, SystemUsers.GuestEmail,
            SystemUsers.GuestUsername, cancellationToken);
    }

    private static async Task EnsureBotAsync(
        ApplicationDbContext db, Guid id, string email, string username, CancellationToken ct)
    {
        var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == id, ct);
        if (exists) return;

        db.Users.Add(User.CreateSystem(id, email, username));
        await db.SaveChangesAsync(ct);

        Console.WriteLine($">>> [SEED] Created system user '{username}' ({id}).");
    }

    private const string DemoWorkspaceName = "Demo Workspace";

    /// <summary>
    /// Seeds a small, lively demo dataset owned by the guest user so a portfolio
    /// visitor lands in a populated workspace. Idempotent: keyed on the demo
    /// workspace (name + guest owner), so it runs once and is a no-op thereafter.
    /// </summary>
    private static async Task SeedDemoDataAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var ownerId = SystemUsers.GuestUserId;
        var botId = SystemUsers.NexusBotId;

        if (await db.Workspaces.AsNoTracking()
                .AnyAsync(w => w.Name == DemoWorkspaceName && w.OwnerId == ownerId, ct))
        {
            return;
        }

        // Workspace + channels. Factory methods generate their own ids, so persist
        // each level before referencing its id on the next.
        var workspace = Workspace.Create(DemoWorkspaceName, "A tour of Nexus — chat, tickets & engineering activity.", ownerId);
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(ct);

        var general = Channel.Create("general", "Team-wide chatter", workspace.Id);
        var engineering = Channel.Create("engineering", "Deploys, PRs & incidents", workspace.Id);
        db.Channels.Add(general);
        db.Channels.Add(engineering);
        await db.SaveChangesAsync(ct);

        db.Messages.Add(Message.Create("Welcome to the Nexus demo! 👋 This workspace is pre-seeded so you can explore.", botId, general.Id));
        db.Messages.Add(Message.Create("Try the Tickets board and the Engineering timeline up top.", botId, general.Id));
        db.Messages.Add(Message.Create("Tip: type /standup or /ticket list in any channel.", botId, engineering.Id));
        db.Messages.Add(Message.Create("Deployed v0.4.0 to staging — watching metrics.", ownerId, engineering.Id));

        // A handful of tickets across statuses so the Kanban board looks alive.
        db.Tickets.Add(Ticket.Create(1, "Set up CI/CD pipeline", "GitHub Actions for build, test and deploy.", TicketStatus.Done, TicketPriority.High, ownerId, workspace.Id));
        db.Tickets.Add(Ticket.Create(2, "Add guest demo login", "One-click 'Try the demo' for portfolio visitors.", TicketStatus.Done, TicketPriority.Medium, ownerId, workspace.Id));
        db.Tickets.Add(Ticket.Create(3, "Postmortem assistant", "Auto-draft postmortems from incident context.", TicketStatus.InProgress, TicketPriority.High, ownerId, workspace.Id));
        db.Tickets.Add(Ticket.Create(4, "Cross-context search", "Full-text search over messages, commits, tickets.", TicketStatus.Open, TicketPriority.Medium, ownerId, workspace.Id));
        db.Tickets.Add(Ticket.Create(5, "Scheduled standups", "Post each user's standup on a cron via Hangfire.", TicketStatus.Open, TicketPriority.Low, ownerId, workspace.Id));
        db.Tickets.Add(Ticket.Create(6, "Investigate flaky webhook test", "Intermittent failure on redelivery dedupe.", TicketStatus.Blocked, TicketPriority.Medium, ownerId, workspace.Id));

        await db.SaveChangesAsync(ct);

        Console.WriteLine($">>> [SEED] Created demo workspace + channels + messages + tickets.");
    }
}
