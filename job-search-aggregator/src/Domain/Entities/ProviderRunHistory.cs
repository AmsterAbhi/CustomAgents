using JobSearchAggregator.Domain.Common;
using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// A single provider's execution within a scheduler run
/// (Phase 2 - Provider Architecture).
/// </summary>
public class ProviderRunHistory : BaseEntity
{
    public required string ProviderName { get; set; }

    public Guid? SchedulerRunHistoryId { get; set; }

    public SchedulerRunHistory? SchedulerRunHistory { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? FinishedAtUtc { get; set; }

    public ProviderRunStatus Status { get; set; } = ProviderRunStatus.Running;

    public int JobsFound { get; set; }

    public int JobsAdded { get; set; }

    public int RetryCount { get; set; }

    public long? DurationMs { get; set; }

    public string? ErrorMessage { get; set; }
}
