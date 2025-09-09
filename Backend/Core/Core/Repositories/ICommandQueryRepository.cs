using Optym.RMX.Core.Entity;

namespace Optym.RMX.Core.Repositories;

/// <summary>
/// Interface for command operations that return entities
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
/// <typeparam name="TEntity">The type of the entity</typeparam>
public interface ICommandQueryRepository<in TId, TEntity> where TEntity : IEntity<TId>
{
    /// <summary>
    /// Inserts a new entity and returns the result
    /// </summary>
    /// <param name="item">The entity to insert</param>
    /// <param name="parameters">Parameters for the insertion</param>
    /// <returns>The inserted entity with any generated values</returns>
    Task<TEntity> InsertItemWithResultAsync(TEntity item, object parameters)
    {
        throw new NotImplementedException();
    }
}
