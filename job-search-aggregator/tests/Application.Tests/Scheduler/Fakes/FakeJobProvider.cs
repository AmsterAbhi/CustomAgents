using JobSearchAggregator.Application.Providers;
using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Application.Tests.Scheduler.Fakes;

/// <summary>
/// Configurable <see cref="IJobProvider"/> test double for exercising the
/// scheduler orchestration handlers without a real network call or DI
/// registration. Never registered in production DI - lives only in the test
/// project.
/// </summary>
public class FakeJobProvider : IJobProvider
{
    private readonly IReadOnlyList<RawJobListing> _listingsToReturn;
    private readonly int _throwCount;
    private int _attempts;

    /// <summary>
    /// Number of times <see cref="FetchJobsAsync"/> has been called so far.
    /// </summary>
    public int CallCount => _attempts;

    public FakeJobProvider(
        string providerName,
        JobSourceType sourceType,
        IReadOnlyList<RawJobListing>? listingsToReturn = null,
        int throwCount = 0)
    {
        ProviderName = providerName;
        SourceType = sourceType;
        _listingsToReturn = listingsToReturn ?? Array.Empty<RawJobListing>();
        _throwCount = throwCount;
    }

    public string ProviderName { get; }

    public JobSourceType SourceType { get; }

    /// <summary>
    /// If <c>true</c>, this provider throws on every call (never succeeds).
    /// </summary>
    public bool ThrowAlways { get; init; }

    public Task<IReadOnlyList<RawJobListing>> FetchJobsAsync(CancellationToken cancellationToken)
    {
        _attempts++;

        if (ThrowAlways || _attempts <= _throwCount)
        {
            throw new InvalidOperationException($"FakeJobProvider '{ProviderName}' configured to fail on attempt {_attempts}.");
        }

        return Task.FromResult(_listingsToReturn);
    }
}
