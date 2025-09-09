using Optym.RMX.Core.Entity;

namespace Optym.RMX.Core.Repositories;

/// <summary>
/// Interface for command operations on entities
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
/// <typeparam name="TEntity">The type of the entity</typeparam>
public interface ICommandRepository<in TId, in TEntity> where TEntity : IEntity<TId>
{
    /// <summary>
    /// Executes a custom command
    /// </summary>
    /// <param name="parameters">Optional parameters for the command</param>
    /// <returns>True if the command executed successfully</returns>
    Task<bool> ExecuteAsync(object? parameters = null);

    /// <summary>
    /// Inserts a new entity
    /// </summary>
    /// <param name="item">The entity to insert</param>
    /// <param name="parameters">Optional parameters for the insertion</param>
    /// <returns>True if the insertion was successful</returns>
    Task<bool> InsertItemAsync(TEntity item, object? parameters = null);

    /// <summary>
    /// Deletes an entity by its identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="parameters">Optional parameters for the deletion</param>
    /// <returns>True if the deletion was successful</returns>
    Task<bool> DeleteItemAsync(TId id, object? parameters = null);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="item">The updated entity</param>
    /// <param name="parameters">Optional parameters for the update</param>
    /// <returns>True if the update was successful</returns>
    Task<bool> UpdateItemAsync(TId id, TEntity item, object? parameters = null);

    /// <summary>
    /// Inserts or updates an entity (upsert operation)
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="item">The entity to upsert</param>
    /// <param name="parameters">Optional parameters for the upsert</param>
    /// <returns>True if the upsert was successful</returns>
    Task<bool> UpsertItemAsync(TId id, TEntity item, object? parameters = null);
}
