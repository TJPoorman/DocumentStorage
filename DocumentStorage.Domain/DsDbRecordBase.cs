using System;

namespace DocumentStorage.Domain;

/// <summary>
/// The <see cref="DsDbRecordBase"/> abstract class serves as the base class for all top-level models
/// in the data schema. It provides a common foundation for entities that represent 
/// records that are top-level models.
/// </summary>
public class DsDbRecordBase : DsRecordBase, IDsDbRecord
{
    /// <inheritdoc/>
    public virtual DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.Now;

    /// <inheritdoc/>
    public virtual string CreatedBy { get; set; }

    /// <inheritdoc/>
    public virtual DateTimeOffset LastModifiedDateTime { get; set; } = DateTimeOffset.Now;

    /// <inheritdoc/>
    public virtual string LastModifiedBy { get; set; }
}
