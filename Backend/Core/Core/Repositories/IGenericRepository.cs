using Optym.RMX.Core.Entity;

namespace Optym.RMX.Core.Repositories;

/// <summary>
/// Generic repository interface that combines query, command, and command-query operations
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
/// <typeparam name="TEntity">The type of the entity</typeparam>
public interface IGenericRepository<TId, TEntity> : IQueryRepository<TId, TEntity>,
    ICommandRepository<TId, TEntity>, ICommandQueryRepository<TId, TEntity>
    where TEntity : IEntity<TId>
{
}
