using DocumentStorage.Domain;
using DocumentStorage.Infrastructure.EntityFramework;
using System.Threading.Tasks;

namespace DocumentStorage.DataGrid.EntityFrameworkConnector;

/// <summary>
/// An abstract base class for Entity Framework repositories that handle data grid operations.
/// <para>
/// This class extends the <see cref="EntityFrameworkRepository{TRecord, TContext}"/> and implements the <see cref="IDataGridRepository{TRecord}"/> interface, 
/// providing methods to retrieve paginated data responses specifically for data grids.
/// </para>
/// </summary>
/// <typeparam name="TRecord">The type of the records handled by the repository, which must implement the <see cref="IDsDbRecord"/> interface.</typeparam>
/// <typeparam name="TContext">The type of the Entity Framework database context, which must inherit from <see cref="EntityFrameworkDbContext"/>.</typeparam>
public abstract class EntityFrameworkDataGridRepository<TRecord, TContext> : EntityFrameworkRepository<TRecord, TContext>, IDataGridRepository<TRecord>
    where TRecord : class, IDsDbRecord
    where TContext : EntityFrameworkDbContext
{
    private readonly EntityFrameworkDataGridQueryService<TRecord, TContext> _dataGridQueryService;

    /// <summary>
    /// Initializes a new instance of the EntityFrameworkDataGridRepository class.
    /// </summary>
    /// <param name="context">The database context for interacting with the data source.</param>
    protected EntityFrameworkDataGridRepository(TContext context) : base(context)
    {
        _dataGridQueryService = new EntityFrameworkDataGridQueryService<TRecord, TContext>(context, IncludeChildren);
    }

    /// <inheritdoc/>
    public virtual async Task<DataGridResponse<TRecord>> GetDataGridResponseAsync(DataGridRequest<TRecord> request) =>
        await _dataGridQueryService.QueryAsync(new DataGridRequest<TRecord>(request)).ConfigureAwait(false);
}
