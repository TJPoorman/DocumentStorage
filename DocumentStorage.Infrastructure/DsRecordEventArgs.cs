using DocumentStorage.Domain;
using System;

namespace DocumentStorage.Infrastructure;

/// <summary>
/// Represents event data for operations affecting a record of type <typeparamref name="T"/>. 
/// This class contains information about the record that was affected.
/// </summary>
/// <typeparam name="T">The type of the record, which must implement <see cref="IDsDbRecord"/>.</typeparam>
public class DsRecordEventArgs<T> : EventArgs
    where T : IDsDbRecord
{
    /// <summary>
    /// Gets the record that was affected by the operation.
    /// </summary>
    public T RecordAffected;

    /// <summary>
    /// Initializes a new instance of the <see cref="DsRecordEventArgs{T}"/> class with the affected record.
    /// </summary>
    /// <param name="recordAffected">The record that was affected by the operation.</param>
    public DsRecordEventArgs(T recordAffected)
    {
        RecordAffected = recordAffected;
    }
}

/// <summary>
/// Represents a method that will handle events related to operations affecting a record of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the record, which must implement <see cref="IDsDbRecord"/>.</typeparam>
/// <param name="sender">The source of the event.</param>
/// <param name="e">A <see cref="DsRecordEventArgs{T}"/> containing the event data.</param>
public delegate void DsRecordEventHandler<T>(object sender, DsRecordEventArgs<T> e) where T : IDsDbRecord;