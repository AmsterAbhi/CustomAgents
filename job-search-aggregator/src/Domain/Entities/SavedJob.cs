using JobSearchAggregator.Domain.Common;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// Tracks that the user saved a job for later review.
/// </summary>
public class SavedJob : BaseEntity
{
    public required Guid JobId { get; set; }

    public Job? Job { get; set; }

    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
