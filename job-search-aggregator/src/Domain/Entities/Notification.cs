using JobSearchAggregator.Domain.Common;
using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// An email notification queued/sent for a highly matched job
/// (Phase 7 - Email Notifications).
/// </summary>
public class Notification : BaseEntity
{
    public required Guid JobId { get; set; }

    public Job? Job { get; set; }

    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;

    public required string Subject { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public decimal MatchPercentAtSend { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public string? ErrorMessage { get; set; }
}
