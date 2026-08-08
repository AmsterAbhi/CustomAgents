namespace JobSearchAggregator.Domain.Enums;

/// <summary>
/// What caused a scheduler run to start.
/// </summary>
public enum SchedulerTriggerType
{
    Automatic = 0,
    Manual = 1,
    RetryFailedProvider = 2
}
