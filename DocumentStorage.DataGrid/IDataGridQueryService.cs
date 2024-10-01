using DocumentStorage.Domain;
using System.Threading.Tasks;

namespace DocumentStorage.DataGrid;

/// <summary>
/// Defines a service interface for querying data grids with records of type <typeparamref name="TRecord"/>.
/// This interface allows asynchronous retrieval of data grid responses based on specified request parameters.
/// </summary>
/// <typeparam name="TRecord">The type of records to be queried, which must implement <see cref="IDsRecord"/>.</typeparam>
public interface IDataGridQueryService<TRecord>
    where TRecord : class, IDsRecord
{
    /// <summary>
    /// Asynchronously queries the data grid and returns a <see cref="DataGridResponse{TRecord}"/> based on the provided request.
    /// </summary>
    /// <param name="request">An instance of <see cref="DataGridRequest{TRecord}"/> containing filters, sorting, and pagination details.</param>
    /// <returns>A task representing the asynchronous operation, containing a <see cref="DataGridResponse{TRecord}"/> with the query results.</returns>
    Task<DataGridResponse<TRecord>> QueryAsync(DataGridRequest<TRecord> request);
}
