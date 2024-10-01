using System.Collections.Generic;

namespace DocumentStorage.DataGrid;

/// <summary>
/// Represents a request for data in a grid-like format, including filters, sorting, and pagination (limit and skip).
/// </summary>
public class DataGridRequest
{
    /// <summary>
    /// List of filter conditions applied to the data.
    /// </summary>
    public virtual List<FilterClause> Filters { get; set; } = new();

    /// <summary>
    /// Maximum number of records to retrieve.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Number of records to skip for pagination.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// List of sorting conditions applied to the data.
    /// </summary>
    public List<SortClause> Sorters { get; set; } = new();
}

/// <summary>
/// Represents a generic data grid request for type <typeparamref name="T"/>, 
/// allowing filters to be strongly typed to <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the data records in the grid.</typeparam>
public class DataGridRequest<T> : DataGridRequest
{
    private List<FilterClause> _filters = new();

    /// <summary>
    /// Gets or sets the filter conditions, converting each <see cref="FilterClause"/> to <see cref="FilterClause{T}"/>.
    /// </summary>
    public override List<FilterClause> Filters
    {
        get => _filters;

        set
        {
            _filters = new List<FilterClause>();

            if (value?.Count < 1) return;

            foreach (FilterClause filter in value)
            {
                _filters.Add(new FilterClause<T>(filter));
            }
        }
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public DataGridRequest() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridRequest{T}"/> class by copying values from another <see cref="DataGridRequest"/>.
    /// </summary>
    /// <param name="request">The request to copy values from.</param>
    public DataGridRequest(DataGridRequest request)
    {
        request ??= new DataGridRequest<T>();

        Filters = request.Filters;
        Limit = request.Limit;
        Skip = request.Skip;
        Sorters = request.Sorters;
    }
}
