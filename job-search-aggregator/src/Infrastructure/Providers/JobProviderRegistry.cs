using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Providers;
using Microsoft.Extensions.Logging;

namespace JobSearchAggregator.Infrastructure.Providers;

/// <summary>
/// Resolves the DI-registered <see cref="IJobProvider"/> collection and
/// filters it down to the providers currently enabled in
/// <c>UserSettings.EnabledProviders</c>.
/// </summary>
public class JobProviderRegistry : IJobProviderRegistry
{
    private readonly IEnumerable<IJobProvider> _providers;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly ILogger<JobProviderRegistry> _logger;

    public JobProviderRegistry(
        IEnumerable<IJobProvider> providers,
        IUserSettingsRepository userSettingsRepository,
        ILogger<JobProviderRegistry> logger)
    {
        _providers = providers;
        _userSettingsRepository = userSettingsRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IJobProvider>> GetEnabledProvidersAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _userSettingsRepository.GetCurrentAsync(cancellationToken);
        var enabledNames = settings.EnabledProviders;

        var providersByName = _providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);

        var enabledProviders = new List<IJobProvider>();
        foreach (var name in enabledNames)
        {
            if (providersByName.TryGetValue(name, out var provider))
            {
                enabledProviders.Add(provider);
            }
            else
            {
                _logger.LogWarning(
                    "UserSettings.EnabledProviders references unknown provider '{ProviderName}' - no matching IJobProvider is registered. Skipping.",
                    name);
            }
        }

        return enabledProviders;
    }
}
