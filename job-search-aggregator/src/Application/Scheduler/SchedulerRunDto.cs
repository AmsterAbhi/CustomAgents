using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Application.Scheduler;

/// <summary>
/// DTO representation of <c>SchedulerRunHistory</c> (with its child provider
/// runs) returned by the scheduler API endpoints.
/// </summary>
public class SchedulerRunDto
{
    public Guid Id { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public SchedulerTriggerType TriggerType { get; set; }

    public SchedulerRunStatus Status { get; set; }

    public int TotalProvidersRun { get; set; }

    public int TotalJobsFound { get; set; }

    public int TotalJobsAdded { get; set; }

    public string? ErrorMessage { get; set; }

    public List<ProviderRunDto> ProviderRuns { get; set; } = new();
}
