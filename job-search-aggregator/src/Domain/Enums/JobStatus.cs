namespace JobSearchAggregator.Domain.Enums;

/// <summary>
/// The current triage state of a job as tracked by the user.
/// </summary>
public enum JobStatus
{
    New = 0,
    Saved = 1,
    Applied = 2,
    Ignored = 3
}
