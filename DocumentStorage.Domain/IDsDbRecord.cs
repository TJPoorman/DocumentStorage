using System;

namespace DocumentStorage.Domain;

/// <summary>
/// Interface used for all top-level model classes.
/// </summary>
public interface IDsDbRecord : IDsRecord
{
    /// <summary>
    /// Gets or sets the timestamp when the record was created.
    /// </summary>
    DateTimeOffset CreatedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or system responsible for creating the record.
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the record was last modified.
    /// </summary>
    DateTimeOffset LastModifiedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user or system responsible
    /// for the most recent modification of the record
    /// </summary>
    string LastModifiedBy { get; set; }
}
