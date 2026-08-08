namespace JobSearchAggregator.Domain.Enums;

/// <summary>
/// Outcome of a single provider execution within a scheduler run.
/// </summary>
public enum ProviderRunStatus
{
    Running = 0,
    Success = 1,
    Failed = 2,
    PartialSuccess = 3
}
