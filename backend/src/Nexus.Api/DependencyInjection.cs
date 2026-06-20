using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexus.Api.Authorization;
using Nexus.Domain.Entities;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

namespace Nexus.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nexus API", Version = "v1" });

            // Handle conflicts in schema names if any
            options.CustomSchemaIds(type => type.FullName);

            // "Authorize" button in Swagger UI — paste the access token (no "Bearer " prefix).
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT access token issued by /api/v1/auth/login or /refresh."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }] = Array.Empty<string>()
            });
        });

        // JWT Bearer authentication. Issuer/audience/secret must match JwtProvider —
        // they're sourced from the same Jwt:* config section so they cannot drift.
        var jwt = configuration.GetSection("Jwt");
        var secret = jwt["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret must be configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"] ?? "Nexus",
                    ValidAudience = jwt["Audience"] ?? "Nexus",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    // Default 5-min skew is overly generous for a 15-min access token —
                    // tighten it so revocation-via-rotation is felt faster.
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                // Allow SignalR to authenticate via access_token query string on the
                // initial WebSocket handshake (browsers can't set Authorization headers
                // on WS upgrades). Restricted to /chatHub so REST endpoints still
                // require the standard Authorization header.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/chatHub", StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Admin is implicitly allowed everywhere an Engineer is — listing both on the
            // RequireEngineer policy avoids hand-rolling that ladder in each handler.
            options.AddPolicy(AuthorizationPolicies.RequireEngineer, p =>
                p.RequireRole(UserRole.Engineer.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(AuthorizationPolicies.RequireAdmin, p =>
                p.RequireRole(UserRole.Admin.ToString()));
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
                // Must carry the same credentials MassTransit uses (see
                // Infrastructure/DependencyInjection UsingRabbitMq); without them the
                // probe falls back to guest/guest and fails on a non-default broker.
                new Uri($"amqp://{configuration["RABBITMQ_USER"] ?? "guest"}:{configuration["RABBITMQ_PASS"] ?? "guest"}@{configuration["RABBITMQ_HOST"] ?? "localhost"}:5672/"),
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
