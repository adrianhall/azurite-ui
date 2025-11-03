using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Services.Azurite.Exceptions;

/// <summary>
/// An exception that is thrown when the resource being accessed does not exist.
/// </summary>
public class ResourceNotFoundException : AzuriteServiceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceNotFoundException()
    {
        StatusCode = StatusCodes.Status404NotFound;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with
    /// a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceNotFoundException(string? message)
        : base(message)
    {
        StatusCode = StatusCodes.Status404NotFound;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with
    /// a specified error message and a reference to the inner exception that is the cause
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceNotFoundException(string? message, Exception? innerException)
        : base(message, innerException)
    {
        StatusCode = StatusCodes.Status404NotFound;
    }

    /// <summary>
    /// The name of the resource that was requested.
    /// </summary>
    public string? ResourceName { get; internal set; }
}