using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Application.Providers;

namespace JobSearchAggregator.Application.Scheduler.Services;

/// <summary>
/// Executes a single provider's fetch-dedup-persist pipeline within a
/// scheduler run. Shared by both <c>RunSchedulerCommand</c> (all enabled
/// providers) and <c>RetryProviderCommand</c> (a single provider) to avoid
/// duplicating the orchestration logic.
/// </summary>
public interface IProviderRunExecutor
{
    /// <summary>
    /// Runs <paramref name="provider"/>'s fetch pipeline (with retry),
    /// persists a <see cref="ProviderRunHistory"/> row for it (linked to
    /// <paramref name="schedulerRunHistoryId"/>), resolves/creates
    /// <c>Company</c> rows, deduplicates against existing <c>Job</c> rows by
    /// <c>UniqueHash</c>, and inserts newly-discovered jobs. Never throws -
    /// failures are recorded on the returned <see cref="ProviderRunHistory"/>
    /// instead.
    /// </summary>
    Task<ProviderRunHistory> ExecuteAsync(IJobProvider provider, Guid schedulerRunHistoryId, CancellationToken cancellationToken);
}
