using System;

namespace DocumentStorage.Infrastructure.EntityFramework;

/// <summary>
/// Represents a transaction in the Entity Framework, implementing IDisposable to manage resource cleanup.
/// This class tracks whether it has been disposed and provides a unique identifier for the transaction.
/// </summary>
public class EntityFrameworkTransaction : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the transaction has been disposed.
    /// </summary>
    public bool Disposed { get; private set; }

    /// <summary>
    /// Gets the unique identifier for the transaction.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <inheritdoc/>
    public void Dispose() => Disposed = true;
}
