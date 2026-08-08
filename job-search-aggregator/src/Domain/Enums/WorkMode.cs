namespace JobSearchAggregator.Domain.Enums;

/// <summary>
/// Where the job is physically performed relative to the employer's office.
/// </summary>
public enum WorkMode
{
    Unspecified = 0,
    Remote = 1,
    Hybrid = 2,
    OnSite = 3
}
