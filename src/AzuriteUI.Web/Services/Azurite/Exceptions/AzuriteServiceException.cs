using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Services.Azurite.Exceptions;

/// <summary>
/// A generic response from the Azurite Service that indicates an error occurred.
/// </summary>
public class AzuriteServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzuriteServiceException"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public AzuriteServiceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzuriteServiceException"/> class with 
    /// a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public AzuriteServiceException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzuriteServiceException"/> class with
    /// a specified error message and a reference to the inner exception that is the cause
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public AzuriteServiceException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// The HTTP status code associated with the exception.  This is provided only
    /// when the Azurite instance responds with an HTTP error code.  If Azurite does
    /// not respond, this will default to 503 Service Unavailable.
    /// </summary>
    public int StatusCode { get; set; } = StatusCodes.Status503ServiceUnavailable;
}