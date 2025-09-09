using Optym.RMX.Core.Entity;
using Optym.RMX.Core.Repositories;

namespace Optym.RMX.Core.Strategy.DataStore;

/// <summary>
/// Interface for data store strategy implementations
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
/// <typeparam name="TEntity">The type of the entity</typeparam>
/// <typeparam name="TPersistenceEntity">The type of the persistence entity</typeparam>
public interface IDataStoreStrategy<TId, TEntity, TPersistenceEntity> : IGenericRepository<TId, TEntity> 
    where TEntity : IEntity<TId>
{
    /// <summary>
    /// Overrides the default collection name for this strategy
    /// </summary>
    /// <param name="collectionName">The collection name to use</param>
    public void OverrideCollectionName(string collectionName);

    /// <summary>
    /// Overrides the default database name for this strategy
    /// </summary>
    /// <param name="databaseName">The database name to use</param>
    public void OverrideDatabaseName(string databaseName);
}
