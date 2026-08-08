using JobSearchAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobSearchAggregator.Infrastructure.Persistence.Configurations;

public class SavedJobConfiguration : IEntityTypeConfiguration<SavedJob>
{
    public void Configure(EntityTypeBuilder<SavedJob> builder)
    {
        builder.ToTable("SavedJobs");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.JobId).IsUnique();

        builder.HasOne(s => s.Job)
            .WithMany(j => j.SavedByUser)
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppliedJobConfiguration : IEntityTypeConfiguration<AppliedJob>
{
    public void Configure(EntityTypeBuilder<AppliedJob> builder)
    {
        builder.ToTable("AppliedJobs");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.JobId).IsUnique();

        builder.HasOne(a => a.Job)
            .WithMany(j => j.AppliedByUser)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class IgnoredJobConfiguration : IEntityTypeConfiguration<IgnoredJob>
{
    public void Configure(EntityTypeBuilder<IgnoredJob> builder)
    {
        builder.ToTable("IgnoredJobs");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.JobId).IsUnique();

        builder.HasOne(i => i.Job)
            .WithMany(j => j.IgnoredByUser)
            .HasForeignKey(i => i.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
