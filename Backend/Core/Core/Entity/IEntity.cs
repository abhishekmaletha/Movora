namespace Optym.RMX.Core.Entity;

/// <summary>
/// Base interface for all entities with an identifier
/// </summary>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
public interface IEntity<TId>
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity
    /// </summary>
    TId Id { get; set; }
}
