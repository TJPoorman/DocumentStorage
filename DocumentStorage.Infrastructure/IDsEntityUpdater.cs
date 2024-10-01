using DocumentStorage.Domain;

namespace DocumentStorage.Infrastructure;

/// <summary>
/// Defines methods for updating and removing database records in a consistent manner across different types of entities.
/// This interface is responsible for updating one record with the values of another and removing a record from the system.
/// </summary>
public interface IDsEntityUpdater
{
    /// <summary>
    /// Updates the target entity with values from the source entity.
    /// This method allows for transferring values between two entities of different types that implement <see cref="IDsDbRecord"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the source entity, which must implement <see cref="IDsDbRecord"/>.</typeparam>
    /// <typeparam name="TTarget">The type of the target entity, which must implement <see cref="IDsDbRecord"/>.</typeparam>
    /// <param name="source">The source entity to update from.</param>
    /// <param name="target">The target entity to be updated.</param>
    void Update<TSource, TTarget>(TTarget source, TSource target)
        where TSource : IDsDbRecord
        where TTarget : IDsDbRecord;

    /// <summary>
    /// Removes the specified record from the system.
    /// </summary>
    /// <param name="record">The record to be removed, which must implement <see cref="IDsRecord"/>.</param>
    void Remove(IDsRecord record);
}
