using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobSearchAggregator.Infrastructure.Persistence.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.PreferredLocations).HasStringListJsonConversion();
        builder.Property(s => s.PreferredRoles).HasStringListJsonConversion();
        builder.Property(s => s.PreferredTechnologies).HasStringListJsonConversion();
        builder.Property(s => s.EnabledProviders).HasStringListJsonConversion();

        builder.Property(s => s.MinimumSalaryLpa).HasColumnType("numeric(10,2)");
        builder.Property(s => s.NotificationThresholdPercent).HasColumnType("numeric(5,2)");
    }
}
