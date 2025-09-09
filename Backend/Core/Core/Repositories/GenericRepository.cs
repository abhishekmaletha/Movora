using Optym.RMX.Core.Entity;
using Optym.RMX.Core.Strategy.DataStore;

namespace Optym.RMX.Core.Repositories;

/// <summary>
/// Generic repository implementation that delegates operations to a data store strategy
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
/// <typeparam name="TEntity">The type of the entity</typeparam>
/// <typeparam name="TPersistanceEntity">The type of the persistence entity</typeparam>
public class GenericRepository<TId, TEntity, TPersistanceEntity> : IGenericRepository<TId, TEntity> 
    where TEntity : IEntity<TId>
{
    readonly protected IDataStoreStrategy<TId, TEntity, TPersistanceEntity> dataStoreStrategy;

    /// <summary>
    /// Initializes a new instance of the GenericRepository class
    /// </summary>
    /// <param name="dataStoreStrategy">The data store strategy to use for operations</param>
    /// <exception cref="ArgumentNullException">Thrown when dataStoreStrategy is null</exception>
    public GenericRepository(IDataStoreStrategy<TId, TEntity, TPersistanceEntity> dataStoreStrategy)
    {
        this.dataStoreStrategy = dataStoreStrategy ?? throw new ArgumentNullException(nameof(dataStoreStrategy));
    }

    /// <inheritdoc />
    public Task<bool> DeleteItemAsync(TId id, object? parameters = null)
    {
        return this.dataStoreStrategy.DeleteItemAsync(id, parameters);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> GetAllAsync(object? parameters = null)
    {
        return await this.dataStoreStrategy.GetAllAsync(parameters);
    }

    /// <inheritdoc />
    public Task<TEntity> GetItemAsync(TId id, object? parameters = null)
    {
        return this.dataStoreStrategy.GetItemAsync(id, parameters);
    }

    /// <inheritdoc />
    public Task<bool> ExecuteAsync(object? parameters = null)
    {
        return this.dataStoreStrategy.ExecuteAsync(parameters);
    }

    /// <inheritdoc />
    public Task<bool> InsertItemAsync(TEntity item, object? parameters = null)
    {
        return this.dataStoreStrategy.InsertItemAsync(item, parameters);
    }

    /// <inheritdoc />
    public Task<TEntity> InsertItemWithResultAsync(TEntity item, object parameters)
    {
        return this.dataStoreStrategy.InsertItemWithResultAsync(item, parameters);
    }

    /// <inheritdoc />
    public Task<bool> UpdateItemAsync(TId id, TEntity item, object? parameters = null)
    {
        return this.dataStoreStrategy.UpdateItemAsync(id, item, parameters);
    }

    /// <inheritdoc />
    public Task<bool> UpsertItemAsync(TId id, TEntity item, object? parameters = null)
    {
        return this.dataStoreStrategy.UpsertItemAsync(id, item, parameters);
    }
}
