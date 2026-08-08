using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Providers;
using JobSearchAggregator.Application.Scheduler.Services;
using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Domain.Enums;
using JobSearchAggregator.Domain.Exceptions;
using Mapster;
using MediatR;

namespace JobSearchAggregator.Application.Scheduler.Commands;

/// <summary>
/// Re-runs a single provider (typically one that previously failed),
/// creating a brand-new <c>SchedulerRunHistory</c> row tagged
/// <see cref="SchedulerTriggerType.RetryFailedProvider"/> rather than
/// reusing the original run.
/// </summary>
public record RetryProviderCommand(Guid SchedulerRunHistoryId, string ProviderName) : IRequest<SchedulerRunDto>;

public class RetryProviderCommandHandler : IRequestHandler<RetryProviderCommand, SchedulerRunDto>
{
    private readonly ISchedulerRunGate _runGate;
    private readonly IJobProviderRegistry _providerRegistry;
    private readonly IProviderRunExecutor _providerRunExecutor;
    private readonly IRepository<SchedulerRunHistory> _schedulerRunHistoryRepository;
    private readonly IRepository<ProviderRunHistory> _providerRunHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RetryProviderCommandHandler(
        ISchedulerRunGate runGate,
        IJobProviderRegistry providerRegistry,
        IProviderRunExecutor providerRunExecutor,
        IRepository<SchedulerRunHistory> schedulerRunHistoryRepository,
        IRepository<ProviderRunHistory> providerRunHistoryRepository,
        IUnitOfWork unitOfWork)
    {
        _runGate = runGate;
        _providerRegistry = providerRegistry;
        _providerRunExecutor = providerRunExecutor;
        _schedulerRunHistoryRepository = schedulerRunHistoryRepository;
        _providerRunHistoryRepository = providerRunHistoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SchedulerRunDto> Handle(RetryProviderCommand request, CancellationToken cancellationToken)
    {
        _ = await _schedulerRunHistoryRepository.GetByIdAsync(request.SchedulerRunHistoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(SchedulerRunHistory), request.SchedulerRunHistoryId);

        // Per architecture doc §5.6: the {id}/{providerName} pair is used only for
        // validation context - it must correspond to an existing Failed (or
        // PartialSuccess) ProviderRunHistory row. A retry cannot target a run that
        // never had that provider fail under it.
        var matchingFailedProviderRuns = await _providerRunHistoryRepository.ListAsync(
            pr => pr.SchedulerRunHistoryId == request.SchedulerRunHistoryId
                && pr.ProviderName == request.ProviderName
                && (pr.Status == ProviderRunStatus.Failed || pr.Status == ProviderRunStatus.PartialSuccess),
            cancellationToken);

        if (matchingFailedProviderRuns.Count == 0)
        {
            throw new NotFoundException(
                nameof(ProviderRunHistory),
                $"SchedulerRunHistoryId={request.SchedulerRunHistoryId}, ProviderName={request.ProviderName}");
        }

        var enabledProviders = await _providerRegistry.GetEnabledProvidersAsync(cancellationToken);
        var provider = enabledProviders.FirstOrDefault(p => string.Equals(p.ProviderName, request.ProviderName, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException(nameof(IJobProvider), request.ProviderName);

        if (!_runGate.TryEnter())
        {
            throw new SchedulerRunInProgressException();
        }

        try
        {
            var retryRun = new SchedulerRunHistory
            {
                StartedAtUtc = DateTime.UtcNow,
                TriggerType = SchedulerTriggerType.RetryFailedProvider,
                Status = SchedulerRunStatus.Running,
            };

            await _schedulerRunHistoryRepository.AddAsync(retryRun, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var providerRun = await _providerRunExecutor.ExecuteAsync(provider, retryRun.Id, cancellationToken);

            retryRun.ProviderRuns.Add(providerRun);
            retryRun.TotalProvidersRun = 1;
            retryRun.TotalJobsFound = providerRun.JobsFound;
            retryRun.TotalJobsAdded = providerRun.JobsAdded;
            retryRun.FinishedAtUtc = DateTime.UtcNow;
            retryRun.Status = providerRun.Status == ProviderRunStatus.Failed
                ? SchedulerRunStatus.Failed
                : SchedulerRunStatus.Success;

            _schedulerRunHistoryRepository.Update(retryRun);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = retryRun.Adapt<SchedulerRunDto>();
            dto.ProviderRuns = retryRun.ProviderRuns.Select(pr => pr.Adapt<ProviderRunDto>()).ToList();
            return dto;
        }
        finally
        {
            _runGate.Release();
        }
    }
}
