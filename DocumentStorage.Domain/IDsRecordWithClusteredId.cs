namespace DocumentStorage.Domain;

/// <summary>
/// The <see cref="IDsRecordWithClusteredId"/> interface extends the <see cref="IDsRecord"/> interface
/// and adds a <see cref="ClusteredId"/> property. This property represents a clustered integer 
/// identifier for the implementing record, typically used as a key in a database.
/// </summary>
public interface IDsRecordWithClusteredId : IDsRecord
{
    /// <summary>
    /// Gets or sets the clustered integer identifier for the record.
    /// </summary>
    int ClusteredId { get; set; }
}
