using JobSearchAggregator.Domain.Common;
using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// A single execution of the scheduler across all enabled providers
/// (Phase 2 - Scheduler).
/// </summary>
public class SchedulerRunHistory : BaseEntity
{
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAtUtc { get; set; }

    public SchedulerTriggerType TriggerType { get; set; } = SchedulerTriggerType.Automatic;

    public SchedulerRunStatus Status { get; set; } = SchedulerRunStatus.Running;

    public int TotalProvidersRun { get; set; }

    public int TotalJobsFound { get; set; }

    public int TotalJobsAdded { get; set; }

    public string? ErrorMessage { get; set; }

    public ICollection<ProviderRunHistory> ProviderRuns { get; set; } = new List<ProviderRunHistory>();
}
