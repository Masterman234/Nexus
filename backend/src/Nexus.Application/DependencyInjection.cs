using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Behaviors;
using Nexus.Application.ChatCommands;
using Nexus.Application.Engineering.Queries.UserActivity;

namespace Nexus.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(IAssemblyMarker).Assembly);

            config.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(IAssemblyMarker).Assembly, includeInternalTypes: true);

        // Cross-context projections (NEX-16). Scoped to match IApplicationDbContext's lifetime.
        services.AddScoped<IUserActivityQuery, UserActivityQuery>();
        services.AddSingleton<Abstractions.IReferenceExtractor, Services.ReferenceExtractor>();

        // NEX-18: slash-command dispatch. Singleton — it captures IServiceScopeFactory
        // and creates its own DI scope per invocation, so it has no scoped state of
        // its own and fits the fire-and-forget background-work pattern.
        services.AddSingleton<IChatCommandRouter, ChatCommandRouter>();

        return services;
    }
}
