namespace JobSearchAggregator.Application.Scheduler.Services;

/// <summary>
/// In-process concurrency gate ensuring only one scheduler run (automatic,
/// manual, or retry-single-provider) executes at a time. Single-node only -
/// no distributed locking, per tech-stack.md's explicit single-node design.
/// </summary>
public interface ISchedulerRunGate
{
    /// <summary>
    /// <c>true</c> if a scheduler run currently holds the gate.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Attempts to acquire the gate without blocking. Returns <c>true</c> if
    /// acquired (caller must call <see cref="Release"/> when done, typically
    /// via try/finally), or <c>false</c> if another run already holds it.
    /// </summary>
    bool TryEnter();

    /// <summary>
    /// Releases the gate. Must only be called by the holder after a
    /// successful <see cref="TryEnter"/>.
    /// </summary>
    void Release();
}
