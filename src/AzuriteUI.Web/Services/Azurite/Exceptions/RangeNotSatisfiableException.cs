namespace AzuriteUI.Web.Services.Azurite.Exceptions;

/// <summary>
/// An exception that is thrown when the resource being downloaded has a range that
/// is outside the size of the resource.
/// </summary>
public class RangeNotSatisfiableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RangeNotSatisfiableException"/> class.
    /// </summary>
    public RangeNotSatisfiableException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeNotSatisfiableException"/> class with 
    /// a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public RangeNotSatisfiableException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeNotSatisfiableException"/> class with
    /// a specified error message and a reference to the inner exception that is the cause
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RangeNotSatisfiableException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}