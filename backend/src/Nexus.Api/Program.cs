using Nexus.Api;
using Nexus.Infrastructure;
using Nexus.Application;
using Nexus.Api.Middleware;
using Nexus.Infrastructure.Realtime;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

// Load .env (walks up from CWD to find the file). Vars become process env vars,
// which ASP.NET's environment-variable configuration source picks up automatically.
// Use double-underscore (`__`) in .env keys to map to nested config (Webhook__GithubSecret -> Webhook:GithubSecret).
// Silent no-op if .env is missing — production hosts inject real env vars instead.
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add Observability (Serilog + OpenTelemetry)
builder.Services.AddObservability(builder.Configuration, builder.Logging);

// Add Layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Apply migrations + seed system users (e.g. github-bot). Idempotent.
await DatabaseInitializer.InitializeAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Respect X-Forwarded-Proto / X-Forwarded-For from upstream proxies (ngrok in dev,
// real reverse proxies in prod). Without this, Kestrel sees ngrok's HTTP hop and
// HttpsRedirection loops the client forever between ngrok (HTTPS) and Kestrel (HTTP).
// KnownNetworks/KnownProxies are cleared so the headers are honored from any source —
// fine in dev; in prod, restrict to your actual proxy CIDR.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies = { }
});

// Custom Middleware
app.UseExceptionHandler();

app.UseCors("AllowFrontend");
app.UseMiddleware<CorrelationIdMiddleware>();

// Skip HTTPS redirection for GitHub webhook deliveries. GitHub will follow a 307
// but some intermediate proxies (incl. ngrok in certain modes) drop request headers
// like X-GitHub-Event during the redirect — so we accept the webhook on whichever
// scheme it arrives on and rely on HMAC signature verification for integrity.
// Exempt any path containing "/webhooks" from HTTPS redirection so an HTTP-arriving
// webhook delivery never triggers a 307 that proxies could mishandle. With
// UseForwardedHeaders above, this is mostly belt-and-suspenders for direct LAN access.
app.UseWhen(
    ctx => !ctx.Request.Path.Value!.Contains("/webhooks", StringComparison.OrdinalIgnoreCase),
    branch => branch.UseHttpsRedirection());

app.UseRateLimiter();

// AuthN before AuthZ; both must run before MapControllers so [Authorize] is honored.
app.UseAuthentication();
app.UseAuthorization();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.MapGet("/api/v{version:apiVersion}/status", () => Results.Ok(new { Status = "Online", Timestamp = DateTime.UtcNow }))
   .WithApiVersionSet(apiVersionSet)
   .MapToApiVersion(1, 0)
   .WithName("GetStatus")
   .WithOpenApi();

app.Run();

#pragma warning disable S1118
public partial class Program { }
#pragma warning restore S1118
