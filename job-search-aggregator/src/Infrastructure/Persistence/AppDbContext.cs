using System.Reflection;
using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAggregator.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core context (PostgreSQL via Npgsql).
/// Implements <see cref="IUnitOfWork"/> directly since EF Core's change
/// tracker already gives us unit-of-work semantics for free.
/// </summary>
public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();

    public DbSet<AppliedJob> AppliedJobs => Set<AppliedJob>();

    public DbSet<IgnoredJob> IgnoredJobs => Set<IgnoredJob>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<UserSkill> UserSkills => Set<UserSkill>();

    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    public DbSet<SchedulerRunHistory> SchedulerRunHistories => Set<SchedulerRunHistory>();

    public DbSet<ProviderRunHistory> ProviderRunHistories => Set<ProviderRunHistory>();

    public DbSet<SystemLogEntry> SystemLogEntries => Set<SystemLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
