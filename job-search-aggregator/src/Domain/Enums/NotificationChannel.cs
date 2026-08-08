namespace JobSearchAggregator.Domain.Enums;

/// <summary>
/// Delivery channel for a notification. Only Email is supported today; the
/// enum leaves room to add Slack/Teams/Push in a later phase.
/// </summary>
public enum NotificationChannel
{
    Email = 0
}
