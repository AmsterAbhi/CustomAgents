using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Application.Scheduler;

/// <summary>
/// DTO representation of <c>ProviderRunHistory</c> returned by the scheduler
/// API endpoints.
/// </summary>
public class ProviderRunDto
{
    public Guid Id { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public Guid? SchedulerRunHistoryId { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public ProviderRunStatus Status { get; set; }

    public int JobsFound { get; set; }

    public int JobsAdded { get; set; }

    public int RetryCount { get; set; }

    public long? DurationMs { get; set; }

    public string? ErrorMessage { get; set; }
}
