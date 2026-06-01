using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Abstractions;
using Nexus.Infrastructure;
using StackExchange.Redis;
using Moq;
using MassTransit;
using Microsoft.Data.Sqlite;

namespace Nexus.IntegrationTests;

#pragma warning disable S3881
public abstract class IntegrationTestBase : IDisposable
#pragma warning restore S3881
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    private readonly SqliteConnection _connection;

    protected IntegrationTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Replace DB with SQLite using the SHARED connection
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlite(_connection);
                    });

                    // Re-register IApplicationDbContext
                    var dbInterfaceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IApplicationDbContext));
                    if (dbInterfaceDescriptor != null) services.Remove(dbInterfaceDescriptor);
                    services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

                    // Mock Redis
                    var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                    if (redisDescriptor != null) services.Remove(redisDescriptor);
                    services.AddSingleton(new Mock<IConnectionMultiplexer>().Object);

                    // Mock MassTransit
                    services.AddMassTransitTestHarness();
                });
            });

        Client = Factory.CreateClient();

        // Ensure DB is created using the shared connection
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
