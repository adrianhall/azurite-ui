using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Services.Azurite.Exceptions;

/// <summary>
/// An exception that is thrown when the resource being created already exists.
/// </summary>
public class ResourceExistsException : AzuriteServiceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceExistsException"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceExistsException()
    {
        StatusCode = StatusCodes.Status409Conflict;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceExistsException"/> class with
    /// a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceExistsException(string? message)
        : base(message)
    {
        StatusCode = StatusCodes.Status409Conflict;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceExistsException"/> class with
    /// a specified error message and a reference to the inner exception that is the cause
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceExistsException(string? message, Exception? innerException)
        : base(message, innerException)
    {
        StatusCode = StatusCodes.Status409Conflict;
    }

    /// <summary>
    /// The name of the resource that was requested.
    /// </summary>
    public string? ResourceName { get; internal set; }
}