namespace JobSearchAggregator.Application.Providers;

/// <summary>
/// Resolves the set of DI-registered <see cref="IJobProvider"/>s that are
/// currently enabled per <c>UserSettings.EnabledProviders</c>.
/// </summary>
public interface IJobProviderRegistry
{
    /// <summary>
    /// Returns the registered providers whose <see cref="IJobProvider.ProviderName"/>
    /// appears in <c>UserSettings.EnabledProviders</c>. Unknown/misspelled
    /// names in <c>EnabledProviders</c> that don't match any registered
    /// provider are ignored with a logged warning, not a hard failure.
    /// </summary>
    Task<IReadOnlyList<IJobProvider>> GetEnabledProvidersAsync(CancellationToken cancellationToken = default);
}
