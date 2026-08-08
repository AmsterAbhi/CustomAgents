using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Application.Providers;

/// <summary>
/// Contract implemented by every concrete job source (Phase 3: Greenhouse,
/// Lever, Ashby, Workday, SmartRecruiters, SuccessFactors, iCIMS, RSS/public
/// API, company career page scrapers). Phase 2 defines the contract only -
/// no production providers are registered yet.
/// </summary>
public interface IJobProvider
{
    /// <summary>
    /// Stable identifier used for <c>UserSettings.EnabledProviders</c>
    /// matching, <c>ProviderRunHistory.ProviderName</c>, and logging.
    /// Convention: matches the <see cref="JobSourceType"/> enum member name
    /// exactly (e.g. "Greenhouse").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Which <see cref="JobSourceType"/> this provider's output should be
    /// tagged with.
    /// </summary>
    JobSourceType SourceType { get; }

    /// <summary>
    /// Fetches all currently-available job listings from this source. Must
    /// NOT throw for "zero results" (return an empty list) - only throw for
    /// genuine failures (network error, non-2xx response, unexpected
    /// schema), which the orchestrator's Polly wrapper will retry and, on
    /// exhaustion, record as a Failed <c>ProviderRunHistory</c>.
    /// </summary>
    Task<IReadOnlyList<RawJobListing>> FetchJobsAsync(CancellationToken cancellationToken);
}
