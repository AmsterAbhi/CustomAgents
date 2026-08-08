using JobSearchAggregator.Application.Scheduler.Services;

namespace JobSearchAggregator.Infrastructure.Scheduler;

/// <summary>
/// <see cref="SemaphoreSlim"/>(1,1)-backed implementation of
/// <see cref="ISchedulerRunGate"/>. Registered as a singleton so the same
/// gate instance is shared across the background service and all API
/// requests within the process.
/// </summary>
public class SchedulerRunGate : ISchedulerRunGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _isRunning;

    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    public bool TryEnter()
    {
        if (!_semaphore.Wait(0))
        {
            return false;
        }

        Volatile.Write(ref _isRunning, 1);
        return true;
    }

    public void Release()
    {
        Volatile.Write(ref _isRunning, 0);
        _semaphore.Release();
    }
}
