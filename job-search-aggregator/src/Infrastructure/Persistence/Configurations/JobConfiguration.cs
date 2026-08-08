using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobSearchAggregator.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title).IsRequired().HasMaxLength(300);
        builder.Property(j => j.Location).IsRequired().HasMaxLength(300);
        builder.Property(j => j.SalaryCurrency).HasMaxLength(10);
        builder.Property(j => j.Department).HasMaxLength(200);
        builder.Property(j => j.Description).IsRequired();
        builder.Property(j => j.ApplyUrl).IsRequired().HasMaxLength(2000);
        builder.Property(j => j.CompanyCareerUrl).HasMaxLength(2000);
        builder.Property(j => j.SourceName).IsRequired().HasMaxLength(200);
        builder.Property(j => j.ExternalId).IsRequired().HasMaxLength(400);
        builder.Property(j => j.UniqueHash).IsRequired().HasMaxLength(128);
        builder.Property(j => j.AiReasoning).HasColumnType("text");

        builder.Property(j => j.RequiredSkills).HasStringListJsonConversion();
        builder.Property(j => j.PreferredSkills).HasStringListJsonConversion();
        builder.Property(j => j.Responsibilities).HasStringListJsonConversion();
        builder.Property(j => j.Benefits).HasStringListJsonConversion();
        builder.Property(j => j.MissingSkills).HasStringListJsonConversion();
        builder.Property(j => j.RecommendedSkills).HasStringListJsonConversion();

        builder.HasIndex(j => j.UniqueHash).IsUnique();
        builder.HasIndex(j => j.PostedAtUtc);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.Source);

        builder.HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
