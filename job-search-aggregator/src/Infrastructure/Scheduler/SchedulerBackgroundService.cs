using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Scheduler.Commands;
using JobSearchAggregator.Application.Scheduler.Services;
using JobSearchAggregator.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobSearchAggregator.Infrastructure.Scheduler;

/// <summary>
/// Periodic background trigger for scheduler runs. Intentionally "dumb":
/// re-reads <c>UserSettings.SchedulerIntervalHours</c> every iteration (via a
/// fresh DI scope, so a settings change takes effect on the next tick without
/// restarting the app), races the timer tick against a manual
/// <see cref="ISchedulerTriggerSignal"/>, then sends
/// <see cref="RunSchedulerCommand"/> via <see cref="ISender"/> inside its own
/// scope. All actual orchestration logic lives in
/// <c>RunSchedulerCommandHandler</c>.
/// </summary>
public class SchedulerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISchedulerTriggerSignal _triggerSignal;
    private readonly ILogger<SchedulerBackgroundService> _logger;

    public SchedulerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ISchedulerTriggerSignal triggerSignal,
        ILogger<SchedulerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _triggerSignal = triggerSignal;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalHours = await GetSchedulerIntervalHoursAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));
            var timerTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
            var signalTask = _triggerSignal.WaitForSignalAsync(stoppingToken);

            Task completedTask;
            try
            {
                completedTask = await Task.WhenAny(timerTask, signalTask);
                await completedTask;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunSchedulerAsync(stoppingToken);
        }
    }

    private async Task<int> GetSchedulerIntervalHoursAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userSettingsRepository = scope.ServiceProvider.GetRequiredService<IUserSettingsRepository>();
        var settings = await userSettingsRepository.GetCurrentAsync(cancellationToken);
        return settings.SchedulerIntervalHours > 0 ? settings.SchedulerIntervalHours : 12;
    }

    private async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            await sender.Send(new RunSchedulerCommand(SchedulerTriggerType.Automatic), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automatic scheduler run failed unexpectedly.");
        }
    }
}
