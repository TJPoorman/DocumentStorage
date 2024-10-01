using System;

namespace DocumentStorage.Domain;

/// <summary>
/// Interface used for base of all model classes.
/// </summary>
public interface IDsRecord
{
    /// <summary>
    /// Gets or sets the primary record identifier.
    /// </summary>
    Guid Id { get; set; }
}
