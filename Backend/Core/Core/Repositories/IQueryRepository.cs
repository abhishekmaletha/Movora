using Optym.RMX.Core.Entity;

namespace Optym.RMX.Core.Repositories;

/// <summary>
/// Interface for query operations on entities
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
/// <typeparam name="TEntity">The type of the entity</typeparam>
public interface IQueryRepository<in TId, TEntity> where TEntity : IEntity<TId>
{
    /// <summary>
    /// Gets a single entity by its identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="parameters">Optional parameters for the query</param>
    /// <returns>The entity if found</returns>
    Task<TEntity> GetItemAsync(TId id, object? parameters = null);

    /// <summary>
    /// Gets all entities
    /// </summary>
    /// <param name="parameters">Optional parameters for the query</param>
    /// <returns>A collection of all entities</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(object? parameters = null);
}
