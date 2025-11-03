# Exception Filter Implementation Specification

## Overview

This specification outlines the implementation of an Exception Filter to handle exceptions thrown by the AzuriteService and convert them into appropriate HTTP responses with proper status codes and RFC 7807 compliant ProblemDetails bodies.

## Goals

1. **Refactor exception hierarchy** to simplify exception handling logic
2. **Implement a global exception filter** that catches AzuriteService exceptions
3. **Return proper HTTP status codes** based on exception type
4. **Generate RFC 7807 compliant ProblemDetails** responses
5. **Log exceptions appropriately** based on severity
6. **Comprehensive test coverage** with unit, integration, and API tests

## Current State Analysis

### Exception Classes (Services/Azurite/Exceptions)

Currently, there are 4 exception types, but they are NOT part of a unified hierarchy:

1. **AzuriteServiceException** - Base exception with `StatusCode` property (default 503)
   - Inherits from: `Exception`
   - Properties: `StatusCode` (int)
   - Use case: Generic Azurite service errors

2. **ResourceNotFoundException** - Resource (container/blob) not found
   - Inherits from: `Exception` ❌ (should be AzuriteServiceException)
   - Properties: `ResourceName` (string?)
   - HTTP Status: Should be 404

3. **ResourceExistsException** - Resource already exists (creation conflict)
   - Inherits from: `Exception` ❌ (should be AzuriteServiceException)
   - Properties: `ResourceName` (string?)
   - HTTP Status: Should be 409

4. **RangeNotSatisfiableException** - Invalid byte range for download
   - Inherits from: `Exception` ❌ (should be AzuriteServiceException)
   - Properties: None
   - HTTP Status: Should be 416

### Current Error Handling

- **No global exception handling** exists in the application
- Controllers return generic 500 errors when AzuriteService exceptions occur
- One local try-catch in `StorageController.ListContainers` for OData validation only
- No ProblemDetails responses for Azurite errors

## Implementation Plan

## Phase 1: Refactor Exception Hierarchy

### Objective
Refactor the three specific exception types to inherit from `AzuriteServiceException` and set their appropriate HTTP status codes. This dramatically simplifies the filter logic.

### 1.1 Refactor ResourceNotFoundException

**File**: `src/AzuriteUI.Web/Services/Azurite/Exceptions/ResourceNotFoundException.cs`

**Changes**:
- Change base class from `Exception` to `AzuriteServiceException`
- Set `StatusCode = StatusCodes.Status404NotFound` in all constructors
- Preserve existing `ResourceName` property
- Keep all three standard exception constructors

**Implementation Example**:
```csharp
public class ResourceNotFoundException : AzuriteServiceException
{
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceNotFoundException()
    {
        StatusCode = StatusCodes.Status404NotFound;
    }

    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceNotFoundException(string? message)
        : base(message)
    {
        StatusCode = StatusCodes.Status404NotFound;
    }

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
```

### 1.2 Refactor ResourceExistsException

**File**: `src/AzuriteUI.Web/Services/Azurite/Exceptions/ResourceExistsException.cs`

**Changes**:
- Change base class from `Exception` to `AzuriteServiceException`
- Set `StatusCode = StatusCodes.Status409Conflict` in all constructors
- Preserve existing `ResourceName` property
- Keep all three standard exception constructors

**Implementation Example**:
```csharp
public class ResourceExistsException : AzuriteServiceException
{
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceExistsException()
    {
        StatusCode = StatusCodes.Status409Conflict;
    }

    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public ResourceExistsException(string? message)
        : base(message)
    {
        StatusCode = StatusCodes.Status409Conflict;
    }

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
```

### 1.3 Refactor RangeNotSatisfiableException

**File**: `src/AzuriteUI.Web/Services/Azurite/Exceptions/RangeNotSatisfiableException.cs`

**Changes**:
- Change base class from `Exception` to `AzuriteServiceException`
- Set `StatusCode = StatusCodes.Status416RangeNotSatisfiable` in all constructors
- Keep all three standard exception constructors

**Implementation Example**:
```csharp
public class RangeNotSatisfiableException : AzuriteServiceException
{
    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public RangeNotSatisfiableException()
    {
        StatusCode = StatusCodes.Status416RangeNotSatisfiable;
    }

    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public RangeNotSatisfiableException(string? message)
        : base(message)
    {
        StatusCode = StatusCodes.Status416RangeNotSatisfiable;
    }

    [ExcludeFromCodeCoverage(Justification = "Standard exception constructor with no additional code")]
    public RangeNotSatisfiableException(string? message, Exception? innerException)
        : base(message, innerException)
    {
        StatusCode = StatusCodes.Status416RangeNotSatisfiable;
    }
}
```

### 1.4 Verify AzuriteService.cs Still Works

**File**: `src/AzuriteUI.Web/Services/Azurite/AzuriteService.cs`

**Verification**: The existing `ConvertAzuriteException` method should continue to work without changes:
```csharp
internal static Exception ConvertAzuriteException(RequestFailedException ex, string? resourceName = null)
{
    return ex.Status switch
    {
        404 => new ResourceNotFoundException(...) { ResourceName = resourceName },
        409 => new ResourceExistsException(...) { ResourceName = resourceName },
        416 => new RangeNotSatisfiableException(...),
        _ => new AzuriteServiceException(...) { StatusCode = ex.Status },
    };
}
```

The StatusCode will be set automatically by the constructors, and then the specific Status from Azure SDK will be preserved in the default case.

### 1.5 Unit Tests for Refactored Exceptions

**Check if tests exist**: Look for unit tests in `tests/AzuriteUI.Web.UnitTests/Services/Azurite/Exceptions/`

**Create or update tests** to verify:
- Each exception type has correct StatusCode after construction
- ResourceName property still works
- Base class properties (Message, InnerException) still work
- Inheritance chain is correct (can catch as AzuriteServiceException)

**Test Example**:
```csharp
[Test]
public void ResourceNotFoundException_ShouldHaveStatusCode404()
{
    // Arrange & Act
    var exception = new ResourceNotFoundException("Test message");

    // Assert
    exception.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    exception.Message.Should().Be("Test message");
}

[Test]
public void ResourceNotFoundException_CanBeCaughtAsAzuriteServiceException()
{
    // Arrange
    Exception caughtException = new ResourceNotFoundException("Test");

    // Act & Assert
    caughtException.Should().BeAssignableTo<AzuriteServiceException>();
    ((AzuriteServiceException)caughtException).StatusCode.Should().Be(404);
}
```

---

## Phase 2: Implement Exception Filter

### Objective
Create a global exception filter that catches `AzuriteServiceException` (including all derived types) and converts them to proper HTTP responses.

### 2.1 Create AzuriteExceptionFilter Class

**File**: `src/AzuriteUI.Web/Filters/AzuriteExceptionFilter.cs`

**Requirements**:
- Implement `IExceptionFilter`
- Check if exception is `AzuriteServiceException`
- Use exception's `StatusCode` property
- Create RFC 7807 compliant ProblemDetails response
- Log with appropriate level based on status code
- Set `ExceptionHandled = true` only for AzuriteServiceException

**Implementation**:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Filters;

/// <summary>
/// Exception filter that handles AzuriteServiceException and converts them
/// to appropriate HTTP responses with ProblemDetails bodies.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Tested through integration tests")]
public class AzuriteExceptionFilter : IExceptionFilter
{
    private readonly ILogger<AzuriteExceptionFilter> _logger;

    public AzuriteExceptionFilter(ILogger<AzuriteExceptionFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
```

### 2.2 Register Filter in Program.cs

**File**: `src/AzuriteUI.Web/Program.cs`

**Change**: Add the filter to the MVC configuration

**Find this line**:
```csharp
builder.Services.AddControllers();
```

**Replace with**:
```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AzuriteExceptionFilter>();
});
```

**Note**: The filter is registered by type, so ASP.NET Core DI will inject the ILogger automatically.

---

## Phase 3: Unit Tests for Exception Filter

### Objective
Create comprehensive unit tests for the `AzuriteExceptionFilter` class.

### 3.1 Create Test Class

**File**: `tests/AzuriteUI.Web.UnitTests/Filters/AzuriteExceptionFilter_Tests.cs`

**Test Structure**:
- Decorate with `[ExcludeFromCodeCoverage]`
- Use regions for each test group
- 15 second timeout per test
- AAA format
- NSubstitute for mocking
- AwesomeAssertions for assertions
- Use `Microsoft.Extensions.Logging.Testing.FakeLogger<T>` for testing logs

### 3.2 Test Scenarios

**Test Coverage**:

1. **Constructor Tests**
   - Throws ArgumentNullException when logger is null

2. **ResourceNotFoundException Tests**
   - Returns 404 status code
   - Creates ProblemDetails with correct structure
   - Includes ResourceName in extensions
   - Logs as Warning
   - Marks exception as handled

3. **ResourceExistsException Tests**
   - Returns 409 status code
   - Creates ProblemDetails with correct structure
   - Includes ResourceName in extensions
   - Logs as Warning
   - Marks exception as handled

4. **RangeNotSatisfiableException Tests**
   - Returns 416 status code
   - Creates ProblemDetails with correct structure
   - Logs as Warning
   - Marks exception as handled

5. **AzuriteServiceException Tests**
   - Returns StatusCode from exception
   - Creates ProblemDetails with correct structure
   - Logs as Error for 5xx codes
   - Logs as Warning for 4xx codes
   - Marks exception as handled

6. **Non-AzuriteServiceException Tests**
   - Does NOT handle other exceptions
   - Does NOT set ExceptionHandled flag
   - Does NOT create result

**Example Test**:
```csharp
using AzuriteUI.Web.Filters;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.UnitTests.Filters;

[TestFixture]
[ExcludeFromCodeCoverage]
public class AzuriteExceptionFilter_Tests
{
    #region Constructor Tests

    [Test]
    [Timeout(15000)]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new AzuriteExceptionFilter(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region ResourceNotFoundException Tests

    [Test]
    [Timeout(15000)]
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

    [Test]
    [Timeout(15000)]
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
    }

    #endregion

    #region ResourceExistsException Tests

    [Test]
    [Timeout(15000)]
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

    #endregion

    #region RangeNotSatisfiableException Tests

    [Test]
    [Timeout(15000)]
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

    #endregion

    #region AzuriteServiceException Tests

    [Test]
    [Timeout(15000)]
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
    }

    [Test]
    [Timeout(15000)]
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
    }

    [Test]
    [Timeout(15000)]
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
    }

    #endregion

    #region Non-AzuriteServiceException Tests

    [Test]
    [Timeout(15000)]
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
```

---

## Phase 4: API/Integration Tests

### Objective
Create API tests that verify the exception filter works end-to-end with real HTTP requests.

### 4.1 Create API Test Class

**File**: `tests/AzuriteUI.Web.IntegrationTests/API/StorageController_ErrorHandling_Tests.cs`

**Test Structure**:
- Use `ServiceFixture` to spin up WebApplicationFactory
- Use real Azurite test container
- Trigger real exceptions by making requests that will fail
- Verify HTTP status codes and response body
- 60 second timeout per test

### 4.2 Test Scenarios

**Test Coverage**:

1. **404 Tests** - Request non-existent container/blob
2. **409 Tests** - Try to create container that already exists
3. **416 Tests** - Request invalid byte range for blob download
4. **503 Tests** - (Optional) Stop Azurite and verify service unavailable

**Example Test**:
```csharp
using AzuriteUI.Web.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AzuriteUI.Web.IntegrationTests.API;

[TestFixture]
[ExcludeFromCodeCoverage]
public class StorageController_ErrorHandling_Tests : IDisposable
{
    private ServiceFixture? _fixture;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new ServiceFixture();
        await _fixture.InitializeAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_fixture != null)
        {
            await _fixture.DisposeAsync();
        }
    }

    #region 404 Not Found Tests

    [Test]
    [Timeout(60000)]
    public async Task GetContainer_NonExistentContainer_Returns404WithProblemDetails()
    {
        // Arrange
        var client = _fixture!.CreateClient();
        var nonExistentContainer = "container-that-does-not-exist-12345";

        // Act
        var response = await client.GetAsync($"/api/containers/{nonExistentContainer}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>();
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        root.GetProperty("title").GetString().Should().Be("Not Found");
        root.GetProperty("detail").GetString().Should().Contain("not found");
        root.GetProperty("instance").GetString().Should().Contain(nonExistentContainer);
    }

    #endregion

    #region 409 Conflict Tests

    [Test]
    [Timeout(60000)]
    public async Task CreateContainer_AlreadyExists_Returns409WithProblemDetails()
    {
        // Arrange
        var client = _fixture!.CreateClient();
        var containerName = await _fixture.Azurite.CreateContainerAsync();

        // Act - Try to create the same container again
        var response = await client.PostAsync($"/api/containers",
            JsonContent.Create(new { name = containerName }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>();
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        root.GetProperty("title").GetString().Should().Be("Conflict");
        root.GetProperty("detail").GetString().Should().Contain("already exists");
        root.GetProperty("resourceName").GetString().Should().Be(containerName);
    }

    #endregion

    #region 416 Range Not Satisfiable Tests

    [Test]
    [Timeout(60000)]
    public async Task DownloadBlob_InvalidRange_Returns416WithProblemDetails()
    {
        // Arrange
        var client = _fixture!.CreateClient();
        var containerName = await _fixture.Azurite.CreateContainerAsync();
        var blobName = await _fixture.Azurite.CreateBlobAsync(containerName, "test.txt", "Hello World");

        // Act - Request bytes beyond the blob size
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}/download");
        request.Headers.Add("Range", "bytes=1000-2000"); // Blob is only ~11 bytes
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>();
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status416RangeNotSatisfiable);
        root.GetProperty("title").GetString().Should().Be("Range Not Satisfiable");
    }

    #endregion

    public void Dispose()
    {
        _fixture?.DisposeAsync().AsTask().Wait();
    }
}
```

**Note**: These tests may need to be adjusted based on which endpoints actually exist in the StorageController. Start with ListContainers endpoint which already exists, then add tests as more endpoints are implemented.

---

## Phase 5: Verification and Coverage

### 5.1 Run Tests

1. **Run unit tests for modified files**:
   ```bash
   dotnet test tests/AzuriteUI.Web.UnitTests/Filters/AzuriteExceptionFilter_Tests.cs
   dotnet test tests/AzuriteUI.Web.UnitTests/Services/Azurite/Exceptions/
   ```

2. **Run API tests**:
   ```bash
   dotnet test tests/AzuriteUI.Web.IntegrationTests/API/StorageController_ErrorHandling_Tests.cs
   ```

3. **Run full test suite**:
   ```bash
   dotnet cake --target=Test
   ```

### 5.2 Analyze Coverage

1. **Check coverage report**:
   ```bash
   # After running dotnet cake, check:
   artifacts/coverage/lcov.info
   ```

2. **Verify coverage for**:
   - All three refactored exception classes (should be mostly excluded due to ExcludeFromCodeCoverage)
   - AzuriteExceptionFilter (should have high coverage from unit tests)
   - Any code paths in AzuriteService that create exceptions

3. **Report any gaps** in coverage and add additional tests if needed

### 5.3 Manual Testing (Optional)

Use a tool like Postman or curl to manually test the API:

```bash
# Test 404
curl -v http://localhost:5000/api/containers/nonexistent

# Test 409 (first create, then try again)
curl -v -X POST http://localhost:5000/api/containers -H "Content-Type: application/json" -d '{"name":"test"}'
curl -v -X POST http://localhost:5000/api/containers -H "Content-Type: application/json" -d '{"name":"test"}'

# Test 416
curl -v -H "Range: bytes=1000-2000" http://localhost:5000/api/containers/test/blobs/myblob/download
```

---

## Acceptance Criteria

### Functional Requirements

- [ ] All three exception types (ResourceNotFoundException, ResourceExistsException, RangeNotSatisfiableException) inherit from AzuriteServiceException
- [ ] Each exception type sets the appropriate StatusCode in constructors
- [ ] ResourceName property is preserved on ResourceNotFoundException and ResourceExistsException
- [ ] AzuriteExceptionFilter is created and implements IExceptionFilter
- [ ] Filter is registered globally in Program.cs
- [ ] Filter returns correct HTTP status codes for each exception type
- [ ] Filter generates RFC 7807 compliant ProblemDetails responses
- [ ] Filter includes ResourceName in extensions when available
- [ ] Filter logs exceptions at appropriate levels (Warning for 4xx, Error for 5xx)
- [ ] Filter only handles AzuriteServiceException (not other exceptions)

### Testing Requirements

- [ ] Unit tests exist for all three refactored exception classes
- [ ] Unit tests verify StatusCode is set correctly
- [ ] Unit tests verify inheritance chain works correctly
- [ ] Unit tests for AzuriteExceptionFilter cover all exception types
- [ ] Unit tests verify ProblemDetails structure is correct
- [ ] Unit tests verify logging behavior
- [ ] Unit tests verify non-Azurite exceptions are not handled
- [ ] API tests verify end-to-end behavior with real HTTP requests
- [ ] API tests cover 404, 409, and 416 scenarios
- [ ] All tests pass with `dotnet test`
- [ ] Full test suite passes with `dotnet cake --target=Test`

### Code Quality Requirements

- [ ] All code follows existing project conventions
- [ ] XML documentation comments on all public members
- [ ] ExcludeFromCodeCoverage attributes on standard exception constructors
- [ ] No TODO or HACK comments in code
- [ ] No compiler warnings
- [ ] Coverage report shows good coverage for new code

### Documentation Requirements

- [ ] This spec document is updated with any design changes discovered during implementation
- [ ] Any deviations from the plan are documented with rationale

---

## References

- [WritingIntegrationTests.spec.md](./WritingIntegrationTests.spec.md) - Integration testing guidelines
- [WritingApiTests.spec.md](./WritingApiTests.spec.md) - API testing guidelines
- [WritingUnitTests.spec.md](./WritingUnitTests.spec.md) - Unit testing guidelines
- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [ASP.NET Core Exception Filters](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters#exception-filters)

---

## Implementation Notes

### Why Not Middleware?

While middleware is another option for global exception handling, an Exception Filter is preferred because:

1. **More granular** - Can be applied per-controller or per-action if needed
2. **Better for APIs** - Designed specifically for MVC/API scenarios
3. **Access to MVC context** - Can access ActionContext, ModelState, etc.
4. **Better testability** - Easier to unit test in isolation
5. **Scoped to controllers** - Only handles exceptions from controller actions

### Why Inherit from AzuriteServiceException?

The refactoring to have all specific exceptions inherit from AzuriteServiceException provides:

1. **Unified handling** - Single check `if (exception is AzuriteServiceException)` catches all types
2. **Self-describing** - Each exception carries its HTTP status code
3. **Extensible** - New exception types just inherit and set StatusCode
4. **Type-safe** - Can still catch specific types for specialized handling
5. **Consistent** - All Azurite errors are part of one exception family

### ProblemDetails Extensions

The `resourceName` extension in ProblemDetails provides additional context for 404 and 409 errors. This helps API consumers understand which specific resource caused the error without parsing the error message.

Example response:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "The container 'mycontainer' was not found.",
  "instance": "/api/containers/mycontainer",
  "resourceName": "mycontainer"
}
```

---

## Future Enhancements

### Potential Future Work (Not in Current Scope)

1. **Localization** - Add support for localized error messages
2. **Correlation IDs** - Add correlation ID to ProblemDetails for distributed tracing
3. **Error codes** - Add machine-readable error codes to ProblemDetails
4. **Rate limiting** - Add retry-after headers for 503 responses
5. **Circuit breaker** - Integrate with circuit breaker pattern for upstream service failures
6. **Metrics** - Track exception counts/types for monitoring

These enhancements can be considered in future iterations as the API matures.
