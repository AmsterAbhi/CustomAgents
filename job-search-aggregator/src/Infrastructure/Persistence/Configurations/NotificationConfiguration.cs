using JobSearchAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobSearchAggregator.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Subject).IsRequired().HasMaxLength(500);
        builder.Property(n => n.ErrorMessage).HasColumnType("text");

        builder.HasIndex(n => n.Status);

        builder.HasOne(n => n.Job)
            .WithMany(j => j.Notifications)
            .HasForeignKey(n => n.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
