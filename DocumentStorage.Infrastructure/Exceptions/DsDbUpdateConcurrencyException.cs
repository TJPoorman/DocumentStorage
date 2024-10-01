using System;

namespace DocumentStorage.Infrastructure;

/// <summary>
/// Represents an exception that is thrown when a concurrency conflict occurs during an update operation in the Document Storage system.
/// This exception occurs when two or more processes attempt to update the same data concurrently.
/// </summary>
public class DsDbUpdateConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DsDbUpdateConcurrencyException"/> class.
    /// </summary>
    public DsDbUpdateConcurrencyException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DsDbUpdateConcurrencyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DsDbUpdateConcurrencyException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DsDbUpdateConcurrencyException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public DsDbUpdateConcurrencyException(string message, Exception inner) : base(message, inner) { }
}
