using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Providers;
using JobSearchAggregator.Application.Scheduler.Services;
using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Domain.Enums;
using JobSearchAggregator.Domain.Exceptions;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JobSearchAggregator.Application.Scheduler.Commands;

/// <summary>
/// Triggers a full scheduler run across all currently-enabled job providers.
/// </summary>
/// <param name="TriggerType">
/// <see cref="SchedulerTriggerType.Automatic"/> for the background service's
/// periodic tick, <see cref="SchedulerTriggerType.Manual"/> for an explicit
/// "run now" API call.
/// </param>
public record RunSchedulerCommand(SchedulerTriggerType TriggerType) : IRequest<SchedulerRunDto?>;

public class RunSchedulerCommandHandler : IRequestHandler<RunSchedulerCommand, SchedulerRunDto?>
{
    private readonly ISchedulerRunGate _runGate;
    private readonly IJobProviderRegistry _providerRegistry;
    private readonly IProviderRunExecutor _providerRunExecutor;
    private readonly IRepository<SchedulerRunHistory> _schedulerRunHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RunSchedulerCommandHandler> _logger;

    public RunSchedulerCommandHandler(
        ISchedulerRunGate runGate,
        IJobProviderRegistry providerRegistry,
        IProviderRunExecutor providerRunExecutor,
        IRepository<SchedulerRunHistory> schedulerRunHistoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<RunSchedulerCommandHandler> logger)
    {
        _runGate = runGate;
        _providerRegistry = providerRegistry;
        _providerRunExecutor = providerRunExecutor;
        _schedulerRunHistoryRepository = schedulerRunHistoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SchedulerRunDto?> Handle(RunSchedulerCommand request, CancellationToken cancellationToken)
    {
        if (!_runGate.TryEnter())
        {
            if (request.TriggerType == SchedulerTriggerType.Automatic)
            {
                _logger.LogInformation("Automatic scheduler trigger skipped - a run is already in progress.");
                return null;
            }

            throw new SchedulerRunInProgressException();
        }

        try
        {
            var run = new SchedulerRunHistory
            {
                StartedAtUtc = DateTime.UtcNow,
                TriggerType = request.TriggerType,
                Status = SchedulerRunStatus.Running,
            };

            await _schedulerRunHistoryRepository.AddAsync(run, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var enabledProviders = await _providerRegistry.GetEnabledProvidersAsync(cancellationToken);

            var anyFailures = false;
            var anySuccesses = false;

            foreach (var provider in enabledProviders)
            {
                var providerRun = await _providerRunExecutor.ExecuteAsync(provider, run.Id, cancellationToken);

                run.ProviderRuns.Add(providerRun);
                run.TotalJobsFound += providerRun.JobsFound;
                run.TotalJobsAdded += providerRun.JobsAdded;

                if (providerRun.Status == ProviderRunStatus.Failed)
                {
                    anyFailures = true;
                }
                else
                {
                    anySuccesses = true;
                }
            }

            run.TotalProvidersRun = enabledProviders.Count;
            run.FinishedAtUtc = DateTime.UtcNow;
            run.Status = enabledProviders.Count == 0 || !anyFailures
                ? SchedulerRunStatus.Success
                : anySuccesses
                    ? SchedulerRunStatus.PartialSuccess
                    : SchedulerRunStatus.Failed;

            _schedulerRunHistoryRepository.Update(run);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = run.Adapt<SchedulerRunDto>();
            dto.ProviderRuns = run.ProviderRuns.Select(pr => pr.Adapt<ProviderRunDto>()).ToList();
            return dto;
        }
        finally
        {
            _runGate.Release();
        }
    }
}
