namespace AzuriteUI.Web.Services.Azurite.Exceptions;

/// <summary>
/// An exception that is thrown when the resource being accessed does not exist.
/// </summary>
public class ResourceNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    public ResourceNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with 
    /// a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ResourceNotFoundException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with
    /// a specified error message and a reference to the inner exception that is the cause
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ResourceNotFoundException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}