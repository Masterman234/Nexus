using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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

        // Set environment variables for the test process to ensure they are picked up 
        // by DotNetEnv or the default configuration builder in Program.cs.
        Environment.SetEnvironmentVariable("Jwt__Secret", "a-very-secret-and-long-key-for-testing-purposes-only-32-bytes");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenPepper", "test-pepper");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Nexus");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Nexus");
        Environment.SetEnvironmentVariable("Gemini__BaseUrl", "https://localhost:5001");

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "a-very-secret-and-long-key-for-testing-purposes-only-32-bytes",
                        ["Jwt:RefreshTokenPepper"] = "test-pepper",
                        ["Jwt:Issuer"] = "Nexus",
                        ["Jwt:Audience"] = "Nexus",
                        ["Gemini:BaseUrl"] = "https://localhost:5001"
                    });
                });

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
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
