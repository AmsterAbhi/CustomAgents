namespace JobSearchAggregator.Domain.Exceptions;

/// <summary>
/// Thrown when a scheduler run (manual trigger or single-provider retry) is
/// requested while another run already holds the in-process concurrency
/// gate. The Api's <c>SchedulerController</c> translates this into an HTTP
/// 409 Conflict.
/// </summary>
public class SchedulerRunInProgressException : Exception
{
    public SchedulerRunInProgressException()
        : base("A scheduler run is already in progress. Please wait for it to finish before starting another.")
    {
    }
}
