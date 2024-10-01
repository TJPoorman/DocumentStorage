using DocumentStorage.Domain;
using System.Linq;

namespace DocumentStorage.Infrastructure.EntityFramework;

/// <summary>
/// Defines a repository interface for managing entities in an Entity Framework context.
/// This interface extends <see cref="IDsTransactionalRepository{TRecord}"/> to include transactional capabilities 
/// and provides additional methods specific to Entity Framework operations.
/// </summary>
/// <typeparam name="TRecord">The type of the records that the repository will manage. Must be a class that implements <see cref="IDsDbRecord"/>.</typeparam>
public interface IEntityFrameworkRepository<TRecord> : IDsTransactionalRepository<TRecord>
    where TRecord : class, IDsDbRecord
{
    /// <summary>
    /// Includes related child entities in the query for the specified record type.
    /// This method is intended to facilitate eager loading of related data.
    /// </summary>
    /// <param name="query">The queryable collection of records to include child entities in.</param>
    /// <returns>An <see cref="IQueryable{TRecord}"/> that includes the related child entities.</returns>
    IQueryable<TRecord> IncludeChildren(IQueryable<TRecord> query);
}