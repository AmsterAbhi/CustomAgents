using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Settings.Queries;
using JobSearchAggregator.Domain.Entities;
using Moq;

namespace JobSearchAggregator.Application.Tests.Settings;

public class GetUserSettingsQueryHandlerTests
{
    private readonly Mock<IUserSettingsRepository> _userSettingsRepositoryMock = new();

    [Fact]
    public async Task Handle_ReturnsDto_MappedFromRepositoryEntity()
    {
        var settings = new UserSettings
        {
            PreferredLocations = new List<string> { "Bengaluru", "Remote" },
            MinExperienceYears = 3,
            MaxExperienceYears = 6,
            MinimumSalaryLpa = 15m,
            PostedWithinHours = 48,
            NotificationThresholdPercent = 75m,
            SchedulerIntervalHours = 6,
            PreferredRoles = new List<string> { "Backend Engineer" },
            PreferredTechnologies = new List<string> { "C#", "PostgreSQL" },
            EnabledProviders = new List<string> { "Greenhouse", "Lever" }
        };

        _userSettingsRepositoryMock
            .Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var handler = new GetUserSettingsQueryHandler(_userSettingsRepositoryMock.Object);

        var result = await handler.Handle(new GetUserSettingsQuery(), CancellationToken.None);

        Assert.Equal(settings.Id, result.Id);
        Assert.Equal(settings.PreferredLocations, result.PreferredLocations);
        Assert.Equal(settings.MinExperienceYears, result.MinExperienceYears);
        Assert.Equal(settings.MaxExperienceYears, result.MaxExperienceYears);
        Assert.Equal(settings.MinimumSalaryLpa, result.MinimumSalaryLpa);
        Assert.Equal(settings.PostedWithinHours, result.PostedWithinHours);
        Assert.Equal(settings.NotificationThresholdPercent, result.NotificationThresholdPercent);
        Assert.Equal(settings.SchedulerIntervalHours, result.SchedulerIntervalHours);
        Assert.Equal(settings.PreferredRoles, result.PreferredRoles);
        Assert.Equal(settings.PreferredTechnologies, result.PreferredTechnologies);
        Assert.Equal(settings.EnabledProviders, result.EnabledProviders);
    }

    [Fact]
    public async Task Handle_CallsGetCurrentAsyncExactlyOnce()
    {
        _userSettingsRepositoryMock
            .Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettings());

        var handler = new GetUserSettingsQueryHandler(_userSettingsRepositoryMock.Object);

        await handler.Handle(new GetUserSettingsQuery(), CancellationToken.None);

        _userSettingsRepositoryMock.Verify(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
