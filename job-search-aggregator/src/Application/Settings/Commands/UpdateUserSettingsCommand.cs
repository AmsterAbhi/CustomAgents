using JobSearchAggregator.Application.Common.Interfaces;
using Mapster;
using MediatR;

namespace JobSearchAggregator.Application.Settings.Commands;

/// <summary>
/// Updates the user's job search preferences and app configuration
/// (Settings screen - editable Locations, Salary, Experience, Preferred
/// Roles/Technologies, Notification Threshold, Scheduler interval, Enabled Providers).
/// </summary>
public record UpdateUserSettingsCommand : IRequest<UserSettingsDto>
{
    public required List<string> PreferredLocations { get; init; }

    public required int MinExperienceYears { get; init; }

    public required int MaxExperienceYears { get; init; }

    public required decimal MinimumSalaryLpa { get; init; }

    public required int PostedWithinHours { get; init; }

    public required decimal NotificationThresholdPercent { get; init; }

    public required int SchedulerIntervalHours { get; init; }

    public required List<string> PreferredRoles { get; init; }

    public required List<string> PreferredTechnologies { get; init; }

    public required List<string> EnabledProviders { get; init; }
}

public class UpdateUserSettingsCommandHandler : IRequestHandler<UpdateUserSettingsCommand, UserSettingsDto>
{
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserSettingsCommandHandler(IUserSettingsRepository userSettingsRepository, IUnitOfWork unitOfWork)
    {
        _userSettingsRepository = userSettingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSettingsDto> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _userSettingsRepository.GetCurrentAsync(cancellationToken);

        settings.PreferredLocations = request.PreferredLocations;
        settings.MinExperienceYears = request.MinExperienceYears;
        settings.MaxExperienceYears = request.MaxExperienceYears;
        settings.MinimumSalaryLpa = request.MinimumSalaryLpa;
        settings.PostedWithinHours = request.PostedWithinHours;
        settings.NotificationThresholdPercent = request.NotificationThresholdPercent;
        settings.SchedulerIntervalHours = request.SchedulerIntervalHours;
        settings.PreferredRoles = request.PreferredRoles;
        settings.PreferredTechnologies = request.PreferredTechnologies;
        settings.EnabledProviders = request.EnabledProviders;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        _userSettingsRepository.Update(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settings.Adapt<UserSettingsDto>();
    }
}
