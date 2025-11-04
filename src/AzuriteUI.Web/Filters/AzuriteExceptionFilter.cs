using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Filters;

/// <summary>
/// Exception filter that handles AzuriteServiceException and converts them
/// to appropriate HTTP responses with ProblemDetails bodies.
/// </summary>
public class AzuriteExceptionFilter : IExceptionFilter
{
    private readonly ILogger<AzuriteExceptionFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzuriteExceptionFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public AzuriteExceptionFilter(ILogger<AzuriteExceptionFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when an exception occurs in the action execution pipeline.
    /// </summary>
    /// <param name="context">The exception context.</param>
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AzuriteServiceException azuriteException)
        {
            // Not an Azurite exception - let other handlers deal with it
            return;
        }

        // Log the exception with appropriate level
        LogException(azuriteException);

        // Create ProblemDetails response
        var problemDetails = new ProblemDetails
        {
            Status = azuriteException.StatusCode,
            Title = GetTitle(azuriteException.StatusCode),
            Detail = azuriteException.Message,
            Instance = context.HttpContext.Request.Path
        };

        // Add ResourceName to extensions if available
        if (azuriteException is ResourceNotFoundException notFoundEx && !string.IsNullOrEmpty(notFoundEx.ResourceName))
        {
            problemDetails.Extensions["resourceName"] = notFoundEx.ResourceName;
        }
        else if (azuriteException is ResourceExistsException existsEx && !string.IsNullOrEmpty(existsEx.ResourceName))
        {
            problemDetails.Extensions["resourceName"] = existsEx.ResourceName;
        }

        // Set the result
        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = azuriteException.StatusCode
        };

        // Mark exception as handled
        context.ExceptionHandled = true;
    }

    private void LogException(AzuriteServiceException exception)
    {
        // Log based on status code range
        if (exception.StatusCode >= 500)
        {
            // 5xx errors are server errors - log as Error
            _logger.LogError(exception,
                "Azurite service error (Status {StatusCode}): {Message}",
                exception.StatusCode,
                exception.Message);
        }
        else if (exception.StatusCode >= 400)
        {
            // 4xx errors are client errors - log as Warning
            _logger.LogWarning(exception,
                "Azurite client error (Status {StatusCode}): {Message}",
                exception.StatusCode,
                exception.Message);
        }
        else
        {
            // Unexpected status code - log as Information
            _logger.LogInformation(exception,
                "Azurite exception (Status {StatusCode}): {Message}",
                exception.StatusCode,
                exception.Message);
        }
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status416RangeNotSatisfiable => "Range Not Satisfiable",
            StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
            StatusCodes.Status502BadGateway => "Bad Gateway",
            _ => "Error"
        };
    }
}
