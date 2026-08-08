using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Providers;
using JobSearchAggregator.Application.Scheduler.Services;
using JobSearchAggregator.Domain.Common;
using JobSearchAggregator.Infrastructure.Persistence;
using JobSearchAggregator.Infrastructure.Persistence.Repositories;
using JobSearchAggregator.Infrastructure.Providers;
using JobSearchAggregator.Infrastructure.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobSearchAggregator.Infrastructure;

/// <summary>
/// Composition-root entry point for registering everything the Infrastructure
/// layer provides: the EF Core / PostgreSQL DbContext, Redis distributed
/// cache, and repository implementations. Called once from the Api project's
/// <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("Missing required connection string 'ConnectionStrings:PostgreSql'.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        services.AddScoped<IJobProviderRegistry, JobProviderRegistry>();
        services.AddSingleton<ISchedulerRunGate, SchedulerRunGate>();
        services.AddSingleton<ISchedulerTriggerSignal, SchedulerTriggerSignal>();
        services.AddHostedService<SchedulerBackgroundService>();

        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = configuration["Redis:InstanceName"] ?? "JobSearchAggregator:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
