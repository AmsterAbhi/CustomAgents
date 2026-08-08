using JobSearchAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobSearchAggregator.Infrastructure.Persistence.Configurations;

public class SchedulerRunHistoryConfiguration : IEntityTypeConfiguration<SchedulerRunHistory>
{
    public void Configure(EntityTypeBuilder<SchedulerRunHistory> builder)
    {
        builder.ToTable("SchedulerRunHistories");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ErrorMessage).HasColumnType("text");

        builder.HasIndex(s => s.StartedAtUtc);

        builder.HasMany(s => s.ProviderRuns)
            .WithOne(p => p.SchedulerRunHistory)
            .HasForeignKey(p => p.SchedulerRunHistoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProviderRunHistoryConfiguration : IEntityTypeConfiguration<ProviderRunHistory>
{
    public void Configure(EntityTypeBuilder<ProviderRunHistory> builder)
    {
        builder.ToTable("ProviderRunHistories");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProviderName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ErrorMessage).HasColumnType("text");

        builder.HasIndex(p => p.ProviderName);
        builder.HasIndex(p => p.StartedAtUtc);
    }
}
