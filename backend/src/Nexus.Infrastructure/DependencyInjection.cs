using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using StackExchange.Redis;
using System.Text;

#pragma warning disable S125

using Nexus.Application.Abstractions;
using Nexus.Infrastructure.Authentication;
using Nexus.Infrastructure.Services;
using Nexus.Application.Auth.Consumers;
using Nexus.Application.Webhooks.Consumers;
using Nexus.Application.Tickets.Consumers;

using Nexus.Infrastructure.AI;

namespace Nexus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // AI - direct REST client against the Google Generative Language API (Gemini).
        // The key itself (and model id) are read inside AIService from IConfiguration so a
        // restart picks up changes to .env without rebuilding the DI graph.
        // We register a typed HttpClient so the BaseAddress is set once and connection
        // pooling/lifecycle is handled by IHttpClientFactory.
        var geminiBaseUrl = configuration["Gemini:BaseUrl"];
        if (string.IsNullOrWhiteSpace(geminiBaseUrl))
        {
            throw new InvalidOperationException("Gemini:BaseUrl is not configured. Set it in appsettings.json.");
        }

        services.AddHttpClient<IAIService, AIService>(client =>
        {
            client.BaseAddress = new Uri(geminiBaseUrl);
            // Total ceiling per call across all retries. Picked generously to cover
            // the Resilience pipeline below: 4 attempts × up to 25s each + backoff.
            client.Timeout = TimeSpan.FromSeconds(120);
        })
        // Resilience wraps the HttpClient with a retry + timeout pipeline. The
        // model is Polly v8 but we configure via Microsoft's higher-level API so
        // upgrades stay clean. Defaults are tuned for short request/response;
        // an LLM call needs a much longer per-attempt timeout.
        .AddResilienceHandler("gemini", builder =>
        {
            builder
                // Per-attempt timeout. LLM calls regularly take 5–15s; 25s gives
                // headroom without letting a truly stuck connection hang forever.
                .AddTimeout(TimeSpan.FromSeconds(25))
                // Retry on transient HTTP failures: 5xx (incl. 503 "model overloaded"),
                // 408, 429 (rate-limited), plus network IOException / TaskCanceled.
                // Exponential backoff with jitter prevents thundering-herd retries
                // when Gemini is recovering from a regional spike.
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(r =>
                            (int)r.StatusCode >= 500 ||
                            r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                            r.StatusCode == System.Net.HttpStatusCode.TooManyRequests),
                });
        });

        // Authentication
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret) && Encoding.UTF8.GetByteCount(o.Secret) >= 32,
                "Jwt:Secret is missing or shorter than 32 bytes (HS256 requires ≥256-bit keys).")
            .Validate(o => !string.IsNullOrWhiteSpace(o.RefreshTokenPepper),
                "Jwt:RefreshTokenPepper is required so refresh-token hashes survive a DB-only leak.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddScoped<ICurrentUser, CurrentUser>();

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
            x.AddConsumer<PullRequestMergedConsumer>();

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
