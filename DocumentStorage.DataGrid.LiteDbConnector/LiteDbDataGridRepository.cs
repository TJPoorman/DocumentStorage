using DocumentStorage.Domain;
using DocumentStorage.Infrastructure.LiteDb;
using System.Threading.Tasks;

namespace DocumentStorage.DataGrid.LiteDbConnector;

/// <summary>
/// An abstract base class for Entity Framework repositories that handle data grid operations.
/// <para>
/// This class extends the <see cref="LiteDbDataGridRepository{TRecord, TContext}"/> and implements the <see cref="IDataGridRepository{TRecord}"/> interface, 
/// providing methods to retrieve paginated data responses specifically for data grids.
/// </para>
/// </summary>
/// <typeparam name="TRecord">The type of the records handled by the repository, which must implement the <see cref="IDsDbRecord"/> interface.</typeparam>
/// <typeparam name="TContext">The type of the Entity Framework database context, which must inherit from <see cref="LiteDbContext"/>.</typeparam>
public abstract class LiteDbDataGridRepository<TRecord, TContext> : LiteDbRepository<TRecord, TContext>, IDataGridRepository<TRecord>
    where TRecord : class, IDsDbRecord
    where TContext : LiteDbContext
{
    private readonly LiteDbDataGridQueryService<TRecord, TContext> _dataGridQueryService;

    /// <summary>
    /// Initializes a new instance of the EntityFrameworkDataGridRepository class.
    /// </summary>
    /// <param name="context">The database context for interacting with the data source.</param>
    protected LiteDbDataGridRepository(TContext context) : base(context)
    {
        _dataGridQueryService = new LiteDbDataGridQueryService<TRecord, TContext>(context);
    }

    /// <inheritdoc/>
    public async Task<DataGridResponse<TRecord>> GetDataGridResponseAsync(DataGridRequest<TRecord> request) =>
        await _dataGridQueryService.QueryAsync(new DataGridRequest<TRecord>(request)).ConfigureAwait(false);
}
