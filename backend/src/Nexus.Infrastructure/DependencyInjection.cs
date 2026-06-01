using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

using Nexus.Application.Abstractions;
using Nexus.Infrastructure.Authentication;
using Nexus.Infrastructure.Services;
using Nexus.Application.Auth.Consumers;
using Nexus.Application.Webhooks.Consumers;

namespace Nexus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Authentication
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtProvider, JwtProvider>();

        // Realtime
        services.AddScoped<IChatService, ChatService>();

        // EF Core PostgreSQL
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration["DB_CONNECTION_STRING"];
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Redis
        var redisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { redisConnectionString },
                AbortOnConnectFail = false
            }));

        // SignalR with Redis Backplane
        services.AddSignalR()
                .AddStackExchangeRedis(redisConnectionString, options =>
                {
                    options.Configuration.AbortOnConnectFail = false;
                });

        // MassTransit RabbitMQ
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            // EXPLICIT REGISTRATION to ensure both consumers are active
            x.AddConsumer<UserCreatedConsumer>();
            x.AddConsumer<GithubWebhookConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RABBITMQ_HOST"] ?? "localhost";
                var user = configuration["RABBITMQ_USER"] ?? "guest";
                var pass = configuration["RABBITMQ_PASS"] ?? "guest";

                cfg.Host(host, "/", h =>
                {
                    h.Username(user);
                    h.Password(pass);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
