using FluentValidation;

namespace JobSearchAggregator.Application.Settings.Commands;

public class UpdateUserSettingsCommandValidator : AbstractValidator<UpdateUserSettingsCommand>
{
    public UpdateUserSettingsCommandValidator()
    {
        RuleFor(x => x.PreferredLocations).NotEmpty()
            .WithMessage("At least one preferred location is required.");

        RuleFor(x => x.MinExperienceYears).GreaterThanOrEqualTo(0);

        RuleFor(x => x.MaxExperienceYears).GreaterThanOrEqualTo(x => x.MinExperienceYears)
            .WithMessage("Maximum experience must be greater than or equal to minimum experience.");

        RuleFor(x => x.MinimumSalaryLpa).GreaterThanOrEqualTo(0);

        RuleFor(x => x.PostedWithinHours).GreaterThan(0);

        RuleFor(x => x.NotificationThresholdPercent).InclusiveBetween(0, 100);

        RuleFor(x => x.SchedulerIntervalHours).GreaterThan(0);
    }
}
