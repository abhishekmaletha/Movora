namespace Movora.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<object> CreateAsync(object entity, CancellationToken cancellationToken = default);
    Task<object> UpdateAsync(object entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(object entity, CancellationToken cancellationToken = default);
    Task<object?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<object>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<object?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<object?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<object>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
}