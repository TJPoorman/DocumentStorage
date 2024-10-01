using DocumentStorage.Domain;
using DocumentStorage.Infrastructure;
using System.Threading.Tasks;

namespace DocumentStorage.DataGrid;

/// <summary>
/// Defines a repository interface for handling data grid operations with records of type <typeparamref name="TRecord"/>.
/// This interface extends the <see cref="IDsRepository{TRecord}"/> and provides methods for querying data in a paginated format.
/// </summary>
/// <typeparam name="TRecord">The type of records that the repository will manage, which must implement <see cref="IDsDbRecord"/>.</typeparam>
public interface IDataGridRepository<TRecord> : IDsRepository<TRecord>
    where TRecord : class, IDsDbRecord
{
    /// <summary>
    /// Asynchronously retrieves a <see cref="DataGridResponse{TRecord}"/> based on the specified data grid request parameters.
    /// </summary>
    /// <param name="request">An instance of <see cref="DataGridRequest{TRecord}"/> containing filters, sorting, and pagination information.</param>
    /// <returns>A task representing the asynchronous operation, containing a <see cref="DataGridResponse{TRecord}"/> with the results of the query.</returns>
    Task<DataGridResponse<TRecord>> GetDataGridResponseAsync(DataGridRequest<TRecord> request);
}
