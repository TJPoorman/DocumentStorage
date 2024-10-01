using System.Collections.Generic;

namespace DocumentStorage.DataGrid;

/// <summary>
/// Represents a response for a data grid request, containing the data, total record count, and total page count.
/// </summary>
/// <typeparam name="T">The type of the data records in the response.</typeparam>
public class DataGridResponse<T>
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public DataGridResponse() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridResponse{T}"/> class with the provided data, total record count, and total page count.
    /// </summary>
    /// <param name="data">The data records being returned.</param>
    /// <param name="recordCount">The total number of records in the data source. Defaults to 0 if not provided.</param>
    /// <param name="pageCount">The total number of pages available. Defaults to 0 if not provided.</param>
    public DataGridResponse(IEnumerable<T> data, int recordCount = 0, int pageCount = 0)
    {
        Data = data;
        TotalRecordCount = recordCount;
        TotalPageCount = pageCount;
    }

    /// <summary>
    /// The collection of data records returned in the response.
    /// </summary>
    public IEnumerable<T> Data { get; set; }

    /// <summary>
    /// The total number of records available in the data source.
    /// </summary>
    public int TotalRecordCount { get; set; }

    /// <summary>
    /// The total number of pages available, calculated based on the total record count and page size.
    /// </summary>
    public int TotalPageCount { get; set; }
}