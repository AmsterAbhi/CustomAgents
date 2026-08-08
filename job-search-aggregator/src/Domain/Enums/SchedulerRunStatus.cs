namespace JobSearchAggregator.Domain.Enums;

/// <summary>
/// Outcome of an entire scheduler run across all enabled providers.
/// </summary>
public enum SchedulerRunStatus
{
    Running = 0,
    Success = 1,
    Failed = 2,
    PartialSuccess = 3
}
