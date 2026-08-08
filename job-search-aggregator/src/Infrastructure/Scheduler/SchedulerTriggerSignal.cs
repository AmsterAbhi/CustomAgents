using System.Threading.Channels;
using JobSearchAggregator.Application.Scheduler.Services;

namespace JobSearchAggregator.Infrastructure.Scheduler;

/// <summary>
/// <see cref="Channel{T}"/>-based implementation of
/// <see cref="ISchedulerTriggerSignal"/>. Uses a bounded capacity-1 channel
/// with <see cref="BoundedChannelFullMode.DropWrite"/> so repeated signals
/// while one is already pending are coalesced instead of queuing up
/// unboundedly.
/// </summary>
public class SchedulerTriggerSignal : ISchedulerTriggerSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Signal()
    {
        _channel.Writer.TryWrite(true);
    }

    public async Task WaitForSignalAsync(CancellationToken cancellationToken)
    {
        await _channel.Reader.ReadAsync(cancellationToken);
    }
}
