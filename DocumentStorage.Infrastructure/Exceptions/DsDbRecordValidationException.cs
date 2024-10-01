using System;

namespace DocumentStorage.Infrastructure;

/// <summary>
/// Represents an exception that occurs during validation of a database record in the Document Storage system.
/// This exception is thrown when a validation process fails for a specific record.
/// </summary>
public class DsDbRecordValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DsDbRecordValidationException"/> class.
    /// </summary>
    public DsDbRecordValidationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DsDbRecordValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DsDbRecordValidationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DsDbRecordValidationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public DsDbRecordValidationException(string message, Exception inner) : base(message, inner) { }
}
