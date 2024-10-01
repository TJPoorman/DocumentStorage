namespace DocumentStorage.Domain;

/// <summary>
/// The <see cref="IDsUniqueDbRecord"/> interface extends the <see cref="IDsDbRecord"/> interface
/// and adds a <see cref="UniqueKey"/> property, which represents an alternate unique identifier 
/// for the record, distinct from the primary key.
/// </summary>
/// <remarks>
/// The <see cref="UniqueKey"/> property is used to identify a unique value that makes the record
/// unique within the database, aside from the primary key. This can be useful for enforcing 
/// uniqueness based on business logic, such as a unique code or identifier that is not 
/// tied to the database's primary key constraints.
/// </remarks>
public interface IDsUniqueDbRecord : IDsDbRecord
{
    /// <summary>
    /// Gets or sets the unique key that uniquely identifies the record within the database, 
    /// aside from the primary key.
    /// </summary>
    string UniqueKey { get; set; }
}
