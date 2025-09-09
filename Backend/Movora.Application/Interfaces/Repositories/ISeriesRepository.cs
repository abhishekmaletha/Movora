namespace Movora.Application.Interfaces.Repositories;

public interface ISeriesRepository
{
    Task<object> CreateAsync(object entity, CancellationToken cancellationToken = default);
    Task<object> UpdateAsync(object entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(object entity, CancellationToken cancellationToken = default);
    Task<object?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<object>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<object>> GetByGenreAsync(string genre, CancellationToken cancellationToken = default);
    Task<IEnumerable<object>> SearchByTitleAsync(string title, CancellationToken cancellationToken = default);
    Task<object?> GetWithEpisodesAsync(int seriesId, CancellationToken cancellationToken = default);
}