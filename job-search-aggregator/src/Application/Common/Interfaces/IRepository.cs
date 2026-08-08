using System.Linq.Expressions;
using JobSearchAggregator.Domain.Common;

namespace JobSearchAggregator.Application.Common.Interfaces;

/// <summary>
/// Generic data access abstraction implemented by Infrastructure. Keeps
/// Application handlers free of any EF Core / Npgsql dependency.
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
