using DocumentStorage.Domain;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure;

/// <summary>
/// Defines a generic repository interface for performing CRUD operations on records that implement <see cref="IDsDbRecord"/>.
/// This interface includes asynchronous methods for initializing, retrieving, updating, and deleting records, as well as checking for duplicates.
/// It also provides events for post-upsert and post-delete operations.
/// </summary>
/// <typeparam name="TRecord">The type of the record, which must implement <see cref="IDsDbRecord"/>.</typeparam>
public interface IDsRepository<TRecord>
    where TRecord : class, IDsDbRecord
{
    /// <summary>
    /// Asynchronously deletes a record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the record to be deleted.</param>
    /// <returns>A <see cref="Task{TResult}"/> that returns true if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Asynchronously finds records based on the provided expression filter.
    /// </summary>
    /// <param name="request">An expression used to filter the records.</param>
    /// <returns>A <see cref="Task{TResult}"/> that returns an <see cref="IEnumerable{TRecord}"/> of matching records.</returns>
    Task<IEnumerable<TRecord>> FindAsync(Expression<Func<TRecord, bool>> request);

    /// <summary>
    /// Asynchronously retrieves a record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the record to retrieve.</param>
    /// <returns>A <see cref="Task{TResult}"/> that returns the record if found; otherwise, null.</returns>
    Task<TRecord> GetAsync(Guid id);

    /// <summary>
    /// Asynchronously initializes the repository, typically used for any setup or configuration tasks.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous initialization operation.</returns>
    Task InitializeAsync();

    /// <summary>
    /// Checks if the given record is a duplicate based on the unique key defined in <see cref="IDsUniqueDbRecord"/>.
    /// </summary>
    /// <param name="record">The record to check for duplication.</param>
    /// <returns>True if the record is a duplicate; otherwise, false.</returns>
    bool IsDuplicate(IDsUniqueDbRecord record);

    /// <summary>
    /// Asynchronously inserts or updates the provided record.
    /// </summary>
    /// <param name="record">The record to upsert.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous upsert operation.</returns>
    Task UpsertAsync(TRecord record);

    /// <summary>
    /// Occurs after a record is successfully upserted (inserted or updated).
    /// </summary>
    event DsRecordEventHandler<TRecord> AfterRecordUpserted;

    /// <summary>
    /// Occurs after a record is successfully deleted.
    /// </summary>
    event DsRecordEventHandler<TRecord> AfterRecordDeleted;
}