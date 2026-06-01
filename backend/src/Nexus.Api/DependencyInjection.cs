using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Threading.RateLimiting;

namespace Nexus.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexus API", Version = "v1" });
            
            // Handle conflicts in schema names if any
            options.CustomSchemaIds(type => type.FullName);
        });

        // Infrastructure Health Checks
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "PostgreSQL",
                tags: ["db", "sql", "postgres"])
            .AddRedis(
                configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379",
                name: "Redis",
                tags: ["cache", "redis"])
            .AddRabbitMQ(
                new Uri($"amqp://{configuration["RABBITMQ_HOST"] ?? "localhost"}"),
                name: "RabbitMQ",
                tags: ["messaging", "rabbitmq"]);

        services.AddProblemDetails();
        services.AddExceptionHandler<Middleware.GlobalExceptionHandler>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                builder.WithOrigins("http://localhost:5173")
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

        // API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.Window = TimeSpan.FromSeconds(10);
                opt.PermitLimit = 100;
                opt.QueueLimit = 0;
            });
        });

        return services;
    }

    public static void AddObservability(this IServiceCollection services, IConfiguration configuration, ILoggingBuilder logging)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(configuration["OTEL_SERVICE_NAME"] ?? "Nexus.Api");

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource("MassTransit")
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        // Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        logging.ClearProviders();
        logging.AddSerilog();
    }
}
