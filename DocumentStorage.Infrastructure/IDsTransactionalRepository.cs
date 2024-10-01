using DocumentStorage.Domain;
using System;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure;

/// <summary>
/// Defines a repository interface that supports transaction management operations.
/// Provides methods to begin and commit database transactions asynchronously.
/// </summary>
public interface IDsTransactionalRepository
{
    /// <summary>
    /// Begins a new database transaction asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> that returns an <see cref="IDisposable"/> representing the started transaction.</returns>
    Task<IDisposable> BeginTransactionAsync();

    /// <summary>
    /// Commits the specified database transaction asynchronously.
    /// </summary>
    /// <param name="transaction">The <see cref="IDisposable"/> representing the transaction to commit.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CommitTransactionAsync(IDisposable transaction);
}

/// <summary>
/// Extends the <see cref="IDsRepository{TRecord}"/> interface to support transaction management for a specific record type.
/// Combines repository operations with transaction management for records of type <typeparamref name="TRecord"/>.
/// </summary>
/// <typeparam name="TRecord">The type of the record, which must implement <see cref="IDsDbRecord"/>.</typeparam>
public interface IDsTransactionalRepository<TRecord> : IDsRepository<TRecord>, IDsTransactionalRepository
    where TRecord : class, IDsDbRecord
{ }