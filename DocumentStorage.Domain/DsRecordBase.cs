using System;

namespace DocumentStorage.Domain;

/// <summary>
/// The <see cref="DsRecordBase"/> abstract class serves as the base class for all child models
/// in the data schema. It provides a common foundation for entities that represent 
/// records that are children of a model inheriting from <see cref="DsDbRecordBase"/>.
/// </summary>
public abstract class DsRecordBase : IDsRecord
{
    /// <inheritdoc/>
    public Guid Id { get; set; } = Guid.NewGuid();
}
