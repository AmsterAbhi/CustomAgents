using JobSearchAggregator.Domain.Common;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// Tracks that the user explicitly dismissed a job from consideration.
/// </summary>
public class IgnoredJob : BaseEntity
{
    public required Guid JobId { get; set; }

    public Job? Job { get; set; }

    public DateTime IgnoredAtUtc { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }
}
