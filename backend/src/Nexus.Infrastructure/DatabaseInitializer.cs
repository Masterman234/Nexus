using Microsoft.EntityFrameworkCore;
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
    }

    private static async Task SeedSystemUsersAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        // Idempotent: each bot is checked + added independently so adding a new bot
        // in code doesn't require dropping the existing seed.
        await EnsureBotAsync(db, SystemUsers.GithubBotId, SystemUsers.GithubBotEmail,
            SystemUsers.GithubBotUsername, cancellationToken);
        await EnsureBotAsync(db, SystemUsers.NexusBotId, SystemUsers.NexusBotEmail,
            SystemUsers.NexusBotUsername, cancellationToken);
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
}
