namespace DocumentStorage.DataGrid;

/// <summary>
/// Represents a sorting instruction for a data grid, specifying a field to sort by and the direction of the sort.
/// </summary>
public class SortClause
{
    /// <summary>
    /// Gets or sets the name of the field to sort by.
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Gets or sets the direction of the sort (ascending or descending).
    /// </summary>
    public SortDirection Direction { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SortClause"/> class.
    /// </summary>
    public SortClause() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SortClause"/> class with specified field name and sort direction.
    /// </summary>
    /// <param name="fieldName">The name of the field to sort by.</param>
    /// <param name="direction">The direction of the sort.</param>
    public SortClause(string fieldName, SortDirection direction)
    {
        FieldName = fieldName;
        Direction = direction;
    }
}
