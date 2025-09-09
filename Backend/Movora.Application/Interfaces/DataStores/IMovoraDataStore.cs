using Core.Persistence.DataStoreStrategy;

namespace Movora.Application.Interfaces.DataStores;

public interface IMovoraDataStore : IDataStoreStrategy
{
    // Additional Movora-specific data store operations can be added here
    Task<IEnumerable<T>> VectorSearchAsync<T>(string query, int limit = 10, CancellationToken cancellationToken = default) where T : class;
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
