using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Behaviors;
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

        return services;
    }
}
