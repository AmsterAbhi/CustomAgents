using JobSearchAggregator.Infrastructure.Scheduler;
using Xunit;

namespace JobSearchAggregator.Application.Tests.Scheduler;

public class SchedulerRunGateTests
{
    [Fact]
    public void TryEnter_WhenNotHeld_ReturnsTrueAndSetsIsRunning()
    {
        var gate = new SchedulerRunGate();

        var acquired = gate.TryEnter();

        Assert.True(acquired);
        Assert.True(gate.IsRunning);
    }

    [Fact]
    public void TryEnter_WhenAlreadyHeld_ReturnsFalse()
    {
        var gate = new SchedulerRunGate();
        gate.TryEnter();

        var secondAcquire = gate.TryEnter();

        Assert.False(secondAcquire);
    }

    [Fact]
    public void Release_AllowsSubsequentTryEnterToSucceed()
    {
        var gate = new SchedulerRunGate();
        gate.TryEnter();

        gate.Release();
        var reacquired = gate.TryEnter();

        Assert.True(reacquired);
    }

    [Fact]
    public void Release_SetsIsRunningToFalse()
    {
        var gate = new SchedulerRunGate();
        gate.TryEnter();

        gate.Release();

        Assert.False(gate.IsRunning);
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        var gate = new SchedulerRunGate();

        Assert.False(gate.IsRunning);
    }

    [Fact]
    public async Task TryEnter_OnlyOneOfManyConcurrentCallersSucceeds()
    {
        var gate = new SchedulerRunGate();
        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(gate.TryEnter)).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Single(results, r => r);
    }
}
