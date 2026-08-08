using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Settings.Commands;
using JobSearchAggregator.Domain.Entities;
using Moq;

namespace JobSearchAggregator.Application.Tests.Settings;

public class UpdateUserSettingsCommandHandlerTests
{
    private readonly Mock<IUserSettingsRepository> _userSettingsRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private static UpdateUserSettingsCommand CreateCommand() => new()
    {
        PreferredLocations = new List<string> { "Hyderabad" },
        MinExperienceYears = 2,
        MaxExperienceYears = 5,
        MinimumSalaryLpa = 12m,
        PostedWithinHours = 24,
        NotificationThresholdPercent = 80m,
        SchedulerIntervalHours = 12,
        PreferredRoles = new List<string> { "Full Stack Engineer" },
        PreferredTechnologies = new List<string> { "Angular", ".NET" },
        EnabledProviders = new List<string> { "Ashby" }
    };

    [Fact]
    public async Task Handle_UpdatesExistingSettings_AndReturnsMappedDto()
    {
        var existingSettings = new UserSettings();
        _userSettingsRepositoryMock
            .Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);

        var handler = new UpdateUserSettingsCommandHandler(_userSettingsRepositoryMock.Object, _unitOfWorkMock.Object);
        var command = CreateCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.PreferredLocations, result.PreferredLocations);
        Assert.Equal(command.MinExperienceYears, result.MinExperienceYears);
        Assert.Equal(command.MaxExperienceYears, result.MaxExperienceYears);
        Assert.Equal(command.MinimumSalaryLpa, result.MinimumSalaryLpa);
        Assert.Equal(command.PostedWithinHours, result.PostedWithinHours);
        Assert.Equal(command.NotificationThresholdPercent, result.NotificationThresholdPercent);
        Assert.Equal(command.SchedulerIntervalHours, result.SchedulerIntervalHours);
        Assert.Equal(command.PreferredRoles, result.PreferredRoles);
        Assert.Equal(command.PreferredTechnologies, result.PreferredTechnologies);
        Assert.Equal(command.EnabledProviders, result.EnabledProviders);
        Assert.NotNull(existingSettings.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_CallsUpdateAndSaveChangesExactlyOnce()
    {
        var existingSettings = new UserSettings();
        _userSettingsRepositoryMock
            .Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSettings);

        var handler = new UpdateUserSettingsCommandHandler(_userSettingsRepositoryMock.Object, _unitOfWorkMock.Object);

        await handler.Handle(CreateCommand(), CancellationToken.None);

        _userSettingsRepositoryMock.Verify(r => r.Update(existingSettings), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
