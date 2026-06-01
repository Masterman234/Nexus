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

        await db.Database.MigrateAsync(cancellationToken);
        await SeedSystemUsersAsync(db, cancellationToken);
    }

    private static async Task SeedSystemUsersAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var botExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == SystemUsers.GithubBotId, cancellationToken);

        if (botExists) return;

        var bot = User.CreateSystem(
            SystemUsers.GithubBotId,
            SystemUsers.GithubBotEmail,
            SystemUsers.GithubBotUsername);

        db.Users.Add(bot);
        await db.SaveChangesAsync(cancellationToken);

        Console.WriteLine($">>> [SEED] Created system user '{SystemUsers.GithubBotUsername}' ({SystemUsers.GithubBotId}).");
    }
}
