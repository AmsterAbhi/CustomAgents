using JobSearchAggregator.Domain.Common;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// Single-row table holding the user's job search preferences and
/// application-wide configuration editable from the Settings screen.
/// </summary>
public class UserSettings : BaseEntity
{
    public List<string> PreferredLocations { get; set; } = new();

    public int MinExperienceYears { get; set; }

    public int MaxExperienceYears { get; set; }

    public decimal MinimumSalaryLpa { get; set; }

    public int PostedWithinHours { get; set; } = 24;

    public decimal NotificationThresholdPercent { get; set; } = 70m;

    public int SchedulerIntervalHours { get; set; } = 12;

    public List<string> PreferredRoles { get; set; } = new();

    public List<string> PreferredTechnologies { get; set; } = new();

    public List<string> EnabledProviders { get; set; } = new();
}
