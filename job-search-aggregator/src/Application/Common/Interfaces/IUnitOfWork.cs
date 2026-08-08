namespace JobSearchAggregator.Application.Common.Interfaces;

/// <summary>
/// Coordinates writes across repositories in a single database transaction.
/// Implemented by Infrastructure's EF Core <c>AppDbContext</c>.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
