namespace JobSearchAggregator.Application.Scheduler.Services;

/// <summary>
/// Allows the manual "run now" API request to wake up the
/// <c>SchedulerBackgroundService</c>'s wait loop immediately, instead of
/// waiting for the next periodic timer tick.
/// </summary>
public interface ISchedulerTriggerSignal
{
    /// <summary>
    /// Signals the background service to run immediately. Safe to call even
    /// if no one is currently waiting (the signal is buffered).
    /// </summary>
    void Signal();

    /// <summary>
    /// Waits until <see cref="Signal"/> is called or the cancellation token
    /// fires.
    /// </summary>
    Task WaitForSignalAsync(CancellationToken cancellationToken);
}
