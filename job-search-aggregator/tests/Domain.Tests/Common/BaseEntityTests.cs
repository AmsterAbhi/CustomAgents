using JobSearchAggregator.Domain.Entities;

namespace JobSearchAggregator.Domain.Tests.Common;

/// <summary>
/// Verifies the identity/audit defaults provided by <see cref="Domain.Common.BaseEntity"/>,
/// exercised through the concrete <see cref="UserSettings"/> entity.
/// </summary>
public class BaseEntityTests
{
    [Fact]
    public void NewEntity_HasNonEmptyId()
    {
        var entity = new UserSettings();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void NewEntity_GetsDistinctIdsAcrossInstances()
    {
        var first = new UserSettings();
        var second = new UserSettings();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void NewEntity_HasCreatedAtUtcCloseToNow()
    {
        var before = DateTime.UtcNow;

        var entity = new UserSettings();

        var after = DateTime.UtcNow;
        Assert.InRange(entity.CreatedAtUtc, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void NewEntity_HasNullUpdatedAtUtc()
    {
        var entity = new UserSettings();

        Assert.Null(entity.UpdatedAtUtc);
    }
}
