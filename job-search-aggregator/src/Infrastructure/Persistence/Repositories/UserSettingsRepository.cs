using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAggregator.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="UserSettings"/> is a single-row aggregate: this repository
/// guarantees a row always exists instead of making every caller handle "no
/// settings yet" as a special case.
/// </summary>
public class UserSettingsRepository : Repository<UserSettings>, IUserSettingsRepository
{
    public UserSettingsRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<UserSettings> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var settings = await DbSet.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new UserSettings();
        await DbSet.AddAsync(settings, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
