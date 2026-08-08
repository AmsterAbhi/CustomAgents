using JobSearchAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobSearchAggregator.Infrastructure.Persistence.Configurations;

public class SystemLogEntryConfiguration : IEntityTypeConfiguration<SystemLogEntry>
{
    public void Configure(EntityTypeBuilder<SystemLogEntry> builder)
    {
        builder.ToTable("SystemLogEntries");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Level).IsRequired().HasMaxLength(20);
        builder.Property(l => l.Message).IsRequired().HasColumnType("text");
        builder.Property(l => l.Exception).HasColumnType("text");
        builder.Property(l => l.SourceContext).HasMaxLength(500);

        builder.HasIndex(l => l.TimestampUtc);
        builder.HasIndex(l => l.Level);
    }
}
