using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nexus.Infrastructure;

namespace Nexus.UnitTests;

#pragma warning disable S3881
public abstract class TestBase : IDisposable
#pragma warning restore S3881
{
    private readonly SqliteConnection _connection;
    protected readonly ApplicationDbContext DbContext;

    protected TestBase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new ApplicationDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
