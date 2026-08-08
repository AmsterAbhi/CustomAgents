namespace JobSearchAggregator.Domain.Enums;

/// <summary>
/// Delivery state of a queued notification.
/// </summary>
public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
