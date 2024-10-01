namespace DocumentStorage.DataGrid;

/// <summary>
/// Represents the types of filtering operations that can be applied to data.
/// Each value corresponds to a specific comparison or matching operation.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// Filter for values that contain the specified input.
    /// </summary>
    Contains = 10,

    /// <summary>
    /// Filter for values that lie between two specified limits.
    /// </summary>
    Between = 20,

    /// <summary>
    /// Filter for values that are equal to the specified input.
    /// </summary>
    Equals = 30,

    /// <summary>
    /// Filter for values greater than the specified input.
    /// </summary>
    GreaterThan = 40,

    /// <summary>
    /// Filter for values greater than or equal to the specified input.
    /// </summary>
    GreaterThanOrEqualTo = 50,

    /// <summary>
    /// Filter for values that are in a specified list.
    /// </summary>
    In = 60,

    /// <summary>
    /// Filter for values less than the specified input.
    /// </summary>
    LessThan = 70,

    /// <summary>
    /// Filter for values less than or equal to the specified input.
    /// </summary>
    LessThanOrEqualTo = 80,

    /// <summary>
    /// Filter for values that are not equal to the specified input.
    /// </summary>
    NotEqualTo = 90
}