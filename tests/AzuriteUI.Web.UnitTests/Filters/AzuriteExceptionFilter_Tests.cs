using AzuriteUI.Web.Filters;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace AzuriteUI.Web.UnitTests.Filters;

/// <summary>
/// Unit tests for the AzuriteExceptionFilter class.
/// </summary>
[ExcludeFromCodeCoverage]
public class AzuriteExceptionFilter_Tests
{
    #region Constructor Tests

    [Fact(Timeout = 15000)]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new AzuriteExceptionFilter(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact(Timeout = 15000)]
    public void Constructor_ValidLogger_CreatesInstance()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();

        // Act
        var filter = new AzuriteExceptionFilter(logger);

        // Assert
        filter.Should().NotBeNull();
    }

    #endregion

    #region ResourceNotFoundException Tests

    [Fact(Timeout = 15000)]
    public void OnException_ResourceNotFoundException_Returns404WithProblemDetails()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ResourceNotFoundException("Container 'test' not found")
        {
            ResourceName = "test"
        };

        var context = CreateExceptionContext(exception, "/api/containers/test");

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Detail.Should().Be("Container 'test' not found");
        problemDetails.Instance.Should().Be("/api/containers/test");
        problemDetails.Extensions.Should().ContainKey("resourceName");
        problemDetails.Extensions["resourceName"].Should().Be("test");
    }

    [Fact(Timeout = 15000)]
    public void OnException_ResourceNotFoundException_WithoutResourceName_DoesNotIncludeExtension()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ResourceNotFoundException("Container not found");
        var context = CreateExceptionContext(exception, "/api/containers");

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Extensions.Should().NotContainKey("resourceName");
    }

    [Fact(Timeout = 15000)]
    public void OnException_ResourceNotFoundException_LogsWarning()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ResourceNotFoundException("Container not found");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Exception.Should().Be(exception);
        logEntry.Message.Should().Contain("Azurite client error");
        logEntry.Message.Should().Contain("404");
    }

    #endregion

    #region ResourceExistsException Tests

    [Fact(Timeout = 15000)]
    public void OnException_ResourceExistsException_Returns409WithProblemDetails()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ResourceExistsException("Container 'test' already exists")
        {
            ResourceName = "test"
        };

        var context = CreateExceptionContext(exception, "/api/containers");

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status409Conflict);
        problemDetails.Title.Should().Be("Conflict");
        problemDetails.Detail.Should().Be("Container 'test' already exists");
        problemDetails.Instance.Should().Be("/api/containers");
        problemDetails.Extensions.Should().ContainKey("resourceName");
        problemDetails.Extensions["resourceName"].Should().Be("test");
    }

    [Fact(Timeout = 15000)]
    public void OnException_ResourceExistsException_WithoutResourceName_DoesNotIncludeExtension()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ResourceExistsException("Resource already exists");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Extensions.Should().NotContainKey("resourceName");
    }

    [Fact(Timeout = 15000)]
    public void OnException_ResourceExistsException_LogsWarning()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ResourceExistsException("Container already exists");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Exception.Should().Be(exception);
        logEntry.Message.Should().Contain("Azurite client error");
        logEntry.Message.Should().Contain("409");
    }

    #endregion

    #region RangeNotSatisfiableException Tests

    [Fact(Timeout = 15000)]
    public void OnException_RangeNotSatisfiableException_Returns416WithProblemDetails()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new RangeNotSatisfiableException("Requested range exceeds blob size");
        var context = CreateExceptionContext(exception, "/api/blobs/download");

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status416RangeNotSatisfiable);

        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status416RangeNotSatisfiable);
        problemDetails.Title.Should().Be("Range Not Satisfiable");
        problemDetails.Detail.Should().Be("Requested range exceeds blob size");
        problemDetails.Instance.Should().Be("/api/blobs/download");
    }

    [Fact(Timeout = 15000)]
    public void OnException_RangeNotSatisfiableException_LogsWarning()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new RangeNotSatisfiableException("Range error");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Exception.Should().Be(exception);
        logEntry.Message.Should().Contain("Azurite client error");
        logEntry.Message.Should().Contain("416");
    }

    [Fact(Timeout = 15000)]
    public void OnException_RangeNotSatisfiableExceptionWithContentLength_SetsContentRangeHeaderAndExtension()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new RangeNotSatisfiableException("Requested range exceeds blob size")
        {
            ContentLength = 1024
        };
        var context = CreateExceptionContext(exception, "/api/containers/test/blobs/file.txt/content");

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();

        // Verify the ContentRange header is set correctly
        context.HttpContext.Response.Headers.Should().ContainKey("Content-Range");
        context.HttpContext.Response.Headers["Content-Range"].ToString().Should().Be("bytes */1024");

        // Verify the ProblemDetails includes contentLength in extensions
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status416RangeNotSatisfiable);

        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status416RangeNotSatisfiable);
        problemDetails.Title.Should().Be("Range Not Satisfiable");
        problemDetails.Detail.Should().Be("Requested range exceeds blob size");
        problemDetails.Instance.Should().Be("/api/containers/test/blobs/file.txt/content");
        problemDetails.Extensions.Should().ContainKey("contentLength");
        problemDetails.Extensions["contentLength"].Should().Be(1024);
    }

    #endregion

    #region AzuriteServiceException Tests

    [Fact(Timeout = 15000)]
    public void OnException_AzuriteServiceException_ReturnsStatusCodeFromException()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new AzuriteServiceException("Service unavailable")
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };

        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();

        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        problemDetails.Title.Should().Be("Service Unavailable");
        problemDetails.Detail.Should().Be("Service unavailable");
    }

    [Fact(Timeout = 15000)]
    public void OnException_AzuriteServiceException502_ReturnsBadGatewayTitle()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new AzuriteServiceException("Bad gateway")
        {
            StatusCode = StatusCodes.Status502BadGateway
        };

        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Bad Gateway");
    }

    [Fact(Timeout = 15000)]
    public void OnException_AzuriteServiceExceptionUnknownCode_ReturnsGenericErrorTitle()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new AzuriteServiceException("Unknown error")
        {
            StatusCode = 418 // I'm a teapot
        };

        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Error");
    }

    [Fact(Timeout = 15000)]
    public void OnException_AzuriteServiceException5xx_LogsError()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new AzuriteServiceException("Service error")
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };

        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Error);
        logEntry.Exception.Should().Be(exception);
        logEntry.Message.Should().Contain("Azurite service error");
        logEntry.Message.Should().Contain("503");
    }

    [Fact(Timeout = 15000)]
    public void OnException_AzuriteServiceException502_LogsError()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new AzuriteServiceException("Bad gateway")
        {
            StatusCode = StatusCodes.Status502BadGateway
        };

        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Error);
        logEntry.Exception.Should().Be(exception);
    }

    [Fact(Timeout = 15000)]
    public void OnException_AzuriteServiceException4xx_LogsWarning()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new AzuriteServiceException("Bad request")
        {
            StatusCode = StatusCodes.Status400BadRequest
        };

        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Warning);
        logEntry.Exception.Should().Be(exception);
        logEntry.Message.Should().Contain("Azurite client error");
        logEntry.Message.Should().Contain("400");
    }

    [Fact(Timeout = 15000)]
    public void OnException_AzuriteServiceException3xx_LogsInformation()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new AzuriteServiceException("Redirect")
        {
            StatusCode = StatusCodes.Status302Found
        };

        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var logEntry = logger.LatestRecord;
        logEntry.Level.Should().Be(LogLevel.Information);
        logEntry.Exception.Should().Be(exception);
        logEntry.Message.Should().Contain("Azurite exception");
        logEntry.Message.Should().Contain("302");
    }

    #endregion

    #region Non-AzuriteServiceException Tests

    [Fact(Timeout = 15000)]
    public void OnException_NonAzuriteException_DoesNotHandle()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new InvalidOperationException("Some other error");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeFalse();
        context.Result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void OnException_NonAzuriteException_DoesNotLog()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ArgumentException("Some other error");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        logger.Collector.Count.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public void OnException_NullException_DoesNotThrow()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var context = CreateExceptionContext(new Exception("Test"));
        context.Exception = null!; // Simulate null exception (shouldn't happen but let's be defensive)

        // Act
        var act = () => filter.OnException(context);

        // Assert
        act.Should().NotThrow();
        context.ExceptionHandled.Should().BeFalse();
    }

    #endregion

    #region ProblemDetails Structure Tests

    [Fact(Timeout = 15000)]
    public void OnException_AnyAzuriteException_ProblemDetailsHasRequiredFields()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ResourceNotFoundException("Test error");
        var context = CreateExceptionContext(exception, "/api/test/path");

        // Act
        filter.OnException(context);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;

        problemDetails.Status.Should().NotBeNull();
        problemDetails.Title.Should().NotBeNullOrWhiteSpace();
        problemDetails.Detail.Should().NotBeNullOrWhiteSpace();
        problemDetails.Instance.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Timeout = 15000)]
    public void OnException_AzuriteException_ResultStatusCodeMatchesProblemDetailsStatus()
    {
        // Arrange
        var logger = new FakeLogger<AzuriteExceptionFilter>();
        var filter = new AzuriteExceptionFilter(logger);

        var exception = new ResourceExistsException("Test");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        var problemDetails = result.Value.Should().BeOfType<ProblemDetails>().Subject;

        result.StatusCode.Should().Be(problemDetails.Status);
        result.StatusCode.Should().Be(exception.StatusCode);
    }

    #endregion

    #region Helper Methods

    private static ExceptionContext CreateExceptionContext(Exception exception, string path = "/test")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    #endregion
}
