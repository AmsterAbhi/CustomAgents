using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Scheduler.Services;
using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Domain.Enums;
using MediatR;

namespace JobSearchAggregator.Application.Scheduler.Queries;

/// <summary>
/// Current scheduler status - whether a run is in progress, when the last
/// run happened, and a best-effort estimate of the next automatic run.
/// </summary>
public class SchedulerStatusDto
{
    public bool IsRunning { get; set; }

    public DateTime? LastRunAtUtc { get; set; }

    public DateTime? NextEstimatedRunAtUtc { get; set; }
}

public record GetSchedulerStatusQuery : IRequest<SchedulerStatusDto>;

public class GetSchedulerStatusQueryHandler : IRequestHandler<GetSchedulerStatusQuery, SchedulerStatusDto>
{
    private readonly ISchedulerRunGate _runGate;
    private readonly IRepository<SchedulerRunHistory> _schedulerRunHistoryRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;

    public GetSchedulerStatusQueryHandler(
        ISchedulerRunGate runGate,
        IRepository<SchedulerRunHistory> schedulerRunHistoryRepository,
        IUserSettingsRepository userSettingsRepository)
    {
        _runGate = runGate;
        _schedulerRunHistoryRepository = schedulerRunHistoryRepository;
        _userSettingsRepository = userSettingsRepository;
    }

    public async Task<SchedulerStatusDto> Handle(GetSchedulerStatusQuery request, CancellationToken cancellationToken)
    {
        var allRuns = await _schedulerRunHistoryRepository.ListAsync(cancellationToken);

        var lastRun = allRuns.OrderByDescending(r => r.StartedAtUtc).FirstOrDefault();
        var lastAutomaticRun = allRuns
            .Where(r => r.TriggerType == SchedulerTriggerType.Automatic)
            .OrderByDescending(r => r.StartedAtUtc)
            .FirstOrDefault();

        DateTime? nextEstimatedRunAtUtc = null;
        if (lastAutomaticRun is not null)
        {
            var settings = await _userSettingsRepository.GetCurrentAsync(cancellationToken);
            nextEstimatedRunAtUtc = lastAutomaticRun.StartedAtUtc.AddHours(settings.SchedulerIntervalHours);
        }

        return new SchedulerStatusDto
        {
            IsRunning = _runGate.IsRunning,
            LastRunAtUtc = lastRun?.StartedAtUtc,
            NextEstimatedRunAtUtc = nextEstimatedRunAtUtc,
        };
    }
}
