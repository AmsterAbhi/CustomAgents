using JobSearchAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobSearchAggregator.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.LogoUrl).HasMaxLength(1000);
        builder.Property(c => c.CareerPageUrl).HasMaxLength(1000);
        builder.Property(c => c.Website).HasMaxLength(1000);
        builder.Property(c => c.Industry).HasMaxLength(200);

        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasMany(c => c.Jobs)
            .WithOne(j => j.Company)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
