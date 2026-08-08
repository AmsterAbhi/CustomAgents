using JobSearchAggregator.Domain.Entities;

namespace JobSearchAggregator.Application.Common.Interfaces;

/// <summary>
/// Repository for the single-row <see cref="UserSettings"/> aggregate.
/// </summary>
public interface IUserSettingsRepository : IRepository<UserSettings>
{
    /// <summary>
    /// Returns the one settings row, creating a default row if none exists yet.
    /// </summary>
    Task<UserSettings> GetCurrentAsync(CancellationToken cancellationToken = default);
}
