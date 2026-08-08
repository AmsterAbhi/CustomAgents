using JobSearchAggregator.Domain.Common;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// Tracks that the user applied to a job.
/// </summary>
public class AppliedJob : BaseEntity
{
    public required Guid JobId { get; set; }

    public Job? Job { get; set; }

    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
