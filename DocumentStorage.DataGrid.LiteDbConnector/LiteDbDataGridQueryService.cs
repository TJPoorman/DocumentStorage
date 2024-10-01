using DocumentStorage.Domain;
using DocumentStorage.Infrastructure.LiteDb;
using LiteDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DocumentStorage.DataGrid.LiteDbConnector;

/// <inheritdoc/>
/// <remarks>
/// Represents a service for querying data in a data grid using LiteDB.
/// </remarks>
/// <typeparam name="TRecord">The type of the records being queried, which must implement the <see cref="IDsRecord"/> interface.</typeparam>
/// <typeparam name="TContext">The type of the database context, which must inherit from <see cref="LiteDbContext"/>.</typeparam>
public class LiteDbDataGridQueryService<TRecord, TContext> : IDataGridQueryService<TRecord>
    where TRecord : class, IDsRecord
    where TContext : LiteDbContext
{
    private readonly TContext _context;
    private readonly LiteRepository _repository;
    private readonly int _maxLimit;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteDbDataGridQueryService{TRecord, TContext}"/> class.
    /// </summary>
    /// <param name="context">The database context for querying records.</param>
    /// <param name="maxLimit">The maximum number of records to return (default is 20).</param>
    /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
    public LiteDbDataGridQueryService(TContext context, int maxLimit = 20)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        try
        {
            _repository = _context.Set<TRecord>().LiteRepository;
            _ = _repository.Database;    // Will throw argument null exception if connection string isn't valid.
        }
        catch (ArgumentNullException exception)
        {
            throw new ArgumentNullException("fileName/connectionString", exception);
        }
        _maxLimit = maxLimit;
    }

    /// <inheritdoc/>
    public async Task<DataGridResponse<TRecord>> QueryAsync(DataGridRequest<TRecord> request) => await QueryAsync(request, null);

    /// <summary>
    /// Asynchronously queries a data grid for a specified collection and request parameters.
    /// </summary>
    /// <typeparam name="T">The type of records being queried.</typeparam>
    /// <param name="request">The data grid request containing filtering, sorting, and pagination details.</param>
    /// <param name="collectionName">The name of the collection to query.</param>
    /// <returns>A task representing the asynchronous operation, with a <see cref="DataGridResponse{T}"/> containing the results.</returns>
    public async Task<DataGridResponse<T>> QueryAsync<T>(DataGridRequest<T> request, string collectionName)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<BsonExpression> queries = new();
        queries.AddRange(GetFilters(request));

        ILiteQueryable<T> queryable = _repository.Query<T>(collectionName);

        AddQueries();

        DataGridResponse<T> response = new()
        {
            TotalRecordCount = queryable.Count(),
            TotalPageCount = queryable.Count() > 0 ? ((int)Math.Ceiling(((double)queryable.Count()) / request.Limit)) : 0
        };

        if (request?.Sorters?.Count == 1 && request?.Sorters[0] is SortClause sortClause)
        {
            _repository.EnsureIndex<T>(sortClause.FieldName, $"LOWER($.{GetFieldName<T>(sortClause.FieldName)})");

            int direction = sortClause.Direction == SortDirection.Descending ? Query.Descending : Query.Ascending;

            queryable.OrderBy(GetFieldName<T>(sortClause.FieldName), direction);
        }

        int limit = request.Limit == 0 ? _maxLimit : request.Limit;
        if (request.Limit > 0 && request.Limit < limit) limit = request.Limit;
        queryable.Limit(limit);

        int skip = request?.Skip ?? 0;
        queryable.Skip(skip);

        response.Data = queryable.ToEnumerable();
        return await Task.FromResult(response);

        void AddQueries()
        {
            if (queries.Count > 1)
            {
                queryable = queryable.Where(Query.And(queries.ToArray()));
            }
            else if (queries.Count == 1)
            {
                queryable = queryable.Where(queries[0]);
            }
        }
    }

    /// <summary>
    /// Generates a field expression for querying based on the field name and value type.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="fieldNameOrId">The name of the field or ID to query.</param>
    /// <param name="value">The value associated with the field.</param>
    /// <returns>The generated field expression as a string.</returns>
    private static string GetFieldExpression<T>(string fieldNameOrId, BsonValue value)
    {
        string fieldName = GetFieldName<T>(fieldNameOrId);
        if (value.IsString) return $"LOWER($.{fieldName})";

        if (fieldName.Contains('.')) fieldName = $"$.{fieldName}";

        return fieldName;
    }

    /// <summary>
    /// Retrieves the appropriate field name for a specified field identifier, considering attributes and naming conventions.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="fieldNameOrId">The field identifier to resolve.</param>
    /// <returns>The resolved field name as a string.</returns>
    private static string GetFieldName<T>(string fieldNameOrId)
    {
        var bsonAttribute = typeof(T).GetProperty(fieldNameOrId)?.GetCustomAttribute<BsonFieldAttribute>();
        if (bsonAttribute is not null) return bsonAttribute.Name;

        string fieldName = fieldNameOrId
            .Replace("LOWER($.", string.Empty)
            .Replace("($.", string.Empty)
            .Replace(")", string.Empty)
            .Replace("$.", string.Empty);

        if (fieldName.Equals("id", StringComparison.InvariantCultureIgnoreCase)) fieldName = "_id";

        if (fieldName.EndsWith(".id", StringComparison.InvariantCultureIgnoreCase))
        {
            fieldName = fieldName[..^2];
            fieldName += "_id";
        }
        return fieldName;
    }

    /// <summary>
    /// Converts various object types to their corresponding BsonValue representation for querying.
    /// </summary>
    /// <param name="value">The object to convert to BsonValue.</param>
    /// <returns>The converted BsonValue.</returns>
    private static BsonValue GetBsonValue(object value)
    {
        if (value is null) return new BsonValue();
        if (value is string str) return new BsonValue(str.ToLower());
        if (value is Enum) return new BsonValue(Enum.GetName(value.GetType(), value).ToLower());
        if (value is DateTimeOffset dateTimeOffset) return new BsonValue(dateTimeOffset.UtcDateTime);
        if (value is Guid guid) return new BsonValue(guid);
        return new BsonValue(value);
    }

    /// <summary>
    /// Constructs filter expressions based on the provided data grid request.
    /// </summary>
    /// <typeparam name="T">The type of records in the request.</typeparam>
    /// <param name="request">The data grid request containing filter criteria.</param>
    /// <returns>A list of BsonExpression filters based on the request.</returns>
    private List<BsonExpression> GetFilters<T>(DataGridRequest<T> request)
    {
        List<BsonExpression> filters = new();

        filters.AddRange(GetContainsFilters(request));
        filters.AddRange(GetSingleValueFilters(request));
        filters.AddRange(GetMultiValueFilters(request));

        for (int index = 0; index < filters.Count; ++index)
        {
            if (filters[index] == null)
            {
                filters.RemoveAt(index);
            }
            else
            {
                string fieldName = GetFieldName<T>(filters[index].Fields.First());
                if (fieldName.Equals("_id")) continue;

                if (fieldName == filters[index].Fields.First())
                {
                    _repository.EnsureIndex<T>(fieldName, $"LOWER($.{fieldName})");
                }
                else
                {
                    _repository.EnsureIndex<T>(fieldName, filters[index].Fields.First());
                }
            }
        }
        return filters;
    }

    /// <summary>
    /// Retrieves "Contains" filters from the data grid request.
    /// </summary>
    /// <typeparam name="T">The type of records in the request.</typeparam>
    /// <param name="request">The data grid request containing filter criteria.</param>
    /// <returns>An enumerable collection of BsonExpression filters for "Contains" criteria.</returns>
    private static IEnumerable<BsonExpression> GetContainsFilters<T>(DataGridRequest<T> request) =>
        (request?.Filters?
            .Where((f) => f is not null && f.FilterType == FilterType.Contains && f.Value is string)
            .Select((f) =>
            {
                BsonValue bsonValue = GetBsonValue(f.Value);
                string fieldExpression = GetFieldExpression<T>(f.FieldName, bsonValue);

                return Query.Contains(fieldExpression, bsonValue.AsString);
            })) ?? Enumerable.Empty<BsonExpression>();

    /// <summary>
    /// Retrieves multi-value filters from the data grid request.
    /// </summary>
    /// <typeparam name="T">The type of records in the request.</typeparam>
    /// <param name="request">The data grid request containing filter criteria.</param>
    /// <returns>An enumerable collection of BsonExpression filters for multi-value criteria.</returns>
    private static IEnumerable<BsonExpression> GetMultiValueFilters<T>(DataGridRequest<T> request) =>
        (request?.Filters?
            .Where((f) => f is not null && f.Value is not null && f.Value is not string && f.Value is IEnumerable && (f.FilterType == FilterType.Between || f.FilterType == FilterType.In))
            .Select((f) =>
            {
                BsonArray values = new();
                foreach (object value in f.Value as IEnumerable)
                {
                    values.Add(GetBsonValue(value));
                }

                if (values.Count < 1)
                {
                    return null;
                }

                string fieldExpression = GetFieldExpression<T>(f.FieldName, values[0]);

                return f.FilterType switch
                {
                    FilterType.Between => values.Count == 2 ? Query.Between(fieldExpression, values[0], values[1]) : null,
                    FilterType.In => Query.In(fieldExpression, values),
                    _ => throw new NotSupportedException(),
                };
            })) ?? Enumerable.Empty<BsonExpression>();

    /// <summary>
    /// Retrieves single-value filters from the data grid request.
    /// </summary>
    /// <typeparam name="T">The type of records in the request.</typeparam>
    /// <param name="request">The data grid request containing filter criteria.</param>
    /// <returns>An enumerable collection of BsonExpression filters for single-value criteria.</returns>
    private static IEnumerable<BsonExpression> GetSingleValueFilters<T>(DataGridRequest<T> request) =>
        (request?.Filters?
            .Where((f) =>
                f is not null && f.Value != null &&
                (f.Value is string || f.Value is not IEnumerable) &&
                (f.FilterType == FilterType.Equals || f.FilterType == FilterType.GreaterThan ||
                f.FilterType == FilterType.GreaterThanOrEqualTo || f.FilterType == FilterType.LessThan ||
                f.FilterType == FilterType.LessThanOrEqualTo || f.FilterType == FilterType.NotEqualTo))
            .Select((f) =>
            {
                BsonValue bsonValue = GetBsonValue(f.Value);
                string fieldExpression = GetFieldExpression<T>(f.FieldName, bsonValue);

                return f.FilterType switch
                {
                    FilterType.Equals => Query.EQ(fieldExpression, bsonValue),
                    FilterType.GreaterThan => Query.GT(fieldExpression, bsonValue),
                    FilterType.GreaterThanOrEqualTo => Query.GTE(fieldExpression, bsonValue),
                    FilterType.LessThan => Query.LT(fieldExpression, bsonValue),
                    FilterType.LessThanOrEqualTo => Query.LTE(fieldExpression, bsonValue),
                    FilterType.NotEqualTo => Query.Not(fieldExpression, bsonValue),
                    _ => throw new NotSupportedException(),
                };
            })) ?? Enumerable.Empty<BsonExpression>();
}
