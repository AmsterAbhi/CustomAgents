using FluentValidation;
using JobSearchAggregator.Application.Scheduler.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobSearchAggregator.Application;

/// <summary>
/// Composition-root entry point for registering everything the Application
/// layer needs: MediatR handlers/pipeline behaviours and FluentValidation validators.
/// Called once from the Api project's <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(JobSearchAggregator.Application.Common.Behaviours.ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddSingleton<IJobHashCalculator, JobHashCalculator>();
        services.AddScoped<IProviderRunExecutor, ProviderRunExecutor>();

        return services;
    }
}
