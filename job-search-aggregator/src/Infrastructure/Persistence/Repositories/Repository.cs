using System.Linq.Expressions;
using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAggregator.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository shared by every entity that doesn't need
/// bespoke query logic. Entity-specific repositories (e.g. <see cref="UserSettingsRepository"/>)
/// derive from this to add extra behaviour.
/// </summary>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public Repository(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        DbSet.AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => DbSet.Update(entity);

    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
