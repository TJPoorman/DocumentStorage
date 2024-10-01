using DocumentStorage.Domain;
using DocumentStorage.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentStorage.DataGrid.EntityFrameworkConnector;

/// <inheritdoc/>
/// <remarks>
/// Represents a service for querying data in a data grid using Entity Framework.
/// </remarks>
/// <typeparam name="TRecord">The type of the records being queried, which must implement the <see cref="IDsRecord"/> interface.</typeparam>
/// <typeparam name="TContext">The type of the database context, which must inherit from <see cref="EntityFrameworkDbContext"/>.</typeparam>
public class EntityFrameworkDataGridQueryService<TRecord, TContext> : IDataGridQueryService<TRecord>
    where TRecord : class, IDsRecord
    where TContext : EntityFrameworkDbContext
{
    private readonly TContext _context;
    private readonly Func<IQueryable<TRecord>, IQueryable<TRecord>> _includeChildrenFunc;
    private readonly int _maxLimit;

    /// <summary>
    /// Initializes a new instance of the EntityFrameworkDataGridQueryService class.
    /// </summary>
    /// <param name="context">The database context for querying records.</param>
    /// <param name="includeChildrenFunc">A function to include related entities in the query.</param>
    /// <param name="maxLimit">The maximum number of records to return (default is 20).</param>
    /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
    public EntityFrameworkDataGridQueryService(TContext context, Func<IQueryable<TRecord>, IQueryable<TRecord>> includeChildrenFunc, int maxLimit = 20)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _includeChildrenFunc = includeChildrenFunc;
        _maxLimit = maxLimit;
    }

    /// <inheritdoc/>
    public async Task<DataGridResponse<TRecord>> QueryAsync(DataGridRequest<TRecord> request)
    {
        IQueryable<TRecord> queryable = GetQueryable(request);
        var recordCount = await queryable.CountAsync();

        return new DataGridResponse<TRecord>
        {
            TotalPageCount = recordCount > 0 ? ((int)Math.Ceiling(((double)recordCount) / request.Limit)) : 0,
            TotalRecordCount = recordCount,
            Data = (await GetPaginatedResults(request, queryable)) as IEnumerable<TRecord>
        };
    }

    /// <summary>
    /// Asynchronously retrieves a paginated list of results based on the specified request and queryable.
    /// </summary>
    /// <param name="request">The request object containing pagination information.</param>
    /// <param name="queryable">The IQueryable of records to paginate.</param>
    /// <returns>A Task representing the asynchronous operation, with the paginated results as a list of TRecord.</returns>
    private async Task<IEnumerable<TRecord>> GetPaginatedResults(DataGridRequest request, IQueryable<TRecord> queryable)
    {
        int limit = request.Limit == 0 ? _maxLimit : request.Limit;
        if (request.Limit > 0 && request.Limit < limit) limit = request.Limit;

        int skip = request?.Skip ?? 0;

        queryable = queryable.Skip(skip).Take(limit);

        return await queryable.ToListAsync();
    }

    /// <summary>
    /// Constructs an IQueryable of records based on the specified request, applying necessary filtering, sorting, 
    /// and including related entities.
    /// </summary>
    /// <param name="request">The request object containing filtering and sorting information.</param>
    /// <returns>An IQueryable of TRecord after applying the filters and includes.</returns>
    private IQueryable<TRecord> GetQueryable(DataGridRequest request)
    {
        IQueryable<TRecord> queryable = _context.Set<TRecord>();
        if (request != null)
        {
            queryable = queryable
                .AddSingleValueWhereCriteria(request)
                .AddMultiValueWhereCriteria(request)
                .AddContainsWhereCriteria(request);
        }

        queryable = queryable.AddOrderBy(request);

        queryable = _includeChildrenFunc(queryable);

        return queryable;
    }
}
