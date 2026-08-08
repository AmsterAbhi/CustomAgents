namespace JobSearchAggregator.Application.Settings;

/// <summary>
/// Data transfer object for <see cref="JobSearchAggregator.Domain.Entities.UserSettings"/>,
/// returned by queries and accepted by the update command.
/// </summary>
public class UserSettingsDto
{
    public Guid Id { get; set; }

    public List<string> PreferredLocations { get; set; } = new();

    public int MinExperienceYears { get; set; }

    public int MaxExperienceYears { get; set; }

    public decimal MinimumSalaryLpa { get; set; }

    public int PostedWithinHours { get; set; }

    public decimal NotificationThresholdPercent { get; set; }

    public int SchedulerIntervalHours { get; set; }

    public List<string> PreferredRoles { get; set; } = new();

    public List<string> PreferredTechnologies { get; set; } = new();

    public List<string> EnabledProviders { get; set; } = new();
}
