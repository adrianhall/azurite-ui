using AzuriteUI.Web.Services.Azurite.Exceptions;
using Microsoft.AspNetCore.Http;

namespace AzuriteUI.Web.UnitTests.Services.Azurite.Exceptions;

/// <summary>
/// Tests for the Azurite exception hierarchy to verify the refactored inheritance structure.
/// </summary>
[ExcludeFromCodeCoverage]
public class AzuriteServiceExceptions_Tests
{
    #region ResourceNotFoundException Tests

    [Fact(Timeout = 15000)]
    public void ResourceNotFoundException_DefaultConstructor_ShouldSetStatusCode404()
    {
        // Arrange & Act
        var exception = new ResourceNotFoundException();

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact(Timeout = 15000)]
    public void ResourceNotFoundException_WithMessage_ShouldSetStatusCode404AndMessage()
    {
        // Arrange
        var message = "Container 'test' not found";

        // Act
        var exception = new ResourceNotFoundException(message);

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        exception.Message.Should().Be(message);
    }

    [Fact(Timeout = 15000)]
    public void ResourceNotFoundException_WithMessageAndInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Container 'test' not found";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new ResourceNotFoundException(message, innerException);

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact(Timeout = 15000)]
    public void ResourceNotFoundException_ResourceName_ShouldBeSettable()
    {
        // Arrange
        var resourceName = "mycontainer";
        var exception = new ResourceNotFoundException("Test message");

        // Act
        exception.ResourceName = resourceName;

        // Assert
        exception.ResourceName.Should().Be(resourceName);
    }

    [Fact(Timeout = 15000)]
    public void ResourceNotFoundException_ShouldBeAssignableToAzuriteServiceException()
    {
        // Arrange & Act
        Exception exception = new ResourceNotFoundException("Test");

        // Assert
        exception.Should().BeAssignableTo<AzuriteServiceException>();
        ((AzuriteServiceException)exception).StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    #endregion

    #region ResourceExistsException Tests

    [Fact(Timeout = 15000)]
    public void ResourceExistsException_DefaultConstructor_ShouldSetStatusCode409()
    {
        // Arrange & Act
        var exception = new ResourceExistsException();

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact(Timeout = 15000)]
    public void ResourceExistsException_WithMessage_ShouldSetStatusCode409AndMessage()
    {
        // Arrange
        var message = "Container 'test' already exists";

        // Act
        var exception = new ResourceExistsException(message);

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        exception.Message.Should().Be(message);
    }

    [Fact(Timeout = 15000)]
    public void ResourceExistsException_WithMessageAndInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Container 'test' already exists";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new ResourceExistsException(message, innerException);

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact(Timeout = 15000)]
    public void ResourceExistsException_ResourceName_ShouldBeSettable()
    {
        // Arrange
        var resourceName = "mycontainer";
        var exception = new ResourceExistsException("Test message");

        // Act
        exception.ResourceName = resourceName;

        // Assert
        exception.ResourceName.Should().Be(resourceName);
    }

    [Fact(Timeout = 15000)]
    public void ResourceExistsException_ShouldBeAssignableToAzuriteServiceException()
    {
        // Arrange & Act
        Exception exception = new ResourceExistsException("Test");

        // Assert
        exception.Should().BeAssignableTo<AzuriteServiceException>();
        ((AzuriteServiceException)exception).StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    #endregion

    #region RangeNotSatisfiableException Tests

    [Fact(Timeout = 15000)]
    public void RangeNotSatisfiableException_DefaultConstructor_ShouldSetStatusCode416()
    {
        // Arrange & Act
        var exception = new RangeNotSatisfiableException();

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status416RangeNotSatisfiable);
    }

    [Fact(Timeout = 15000)]
    public void RangeNotSatisfiableException_WithMessage_ShouldSetStatusCode416AndMessage()
    {
        // Arrange
        var message = "Requested range exceeds blob size";

        // Act
        var exception = new RangeNotSatisfiableException(message);

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status416RangeNotSatisfiable);
        exception.Message.Should().Be(message);
    }

    [Fact(Timeout = 15000)]
    public void RangeNotSatisfiableException_WithMessageAndInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Requested range exceeds blob size";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new RangeNotSatisfiableException(message, innerException);

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status416RangeNotSatisfiable);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact(Timeout = 15000)]
    public void RangeNotSatisfiableException_ShouldBeAssignableToAzuriteServiceException()
    {
        // Arrange & Act
        Exception exception = new RangeNotSatisfiableException("Test");

        // Assert
        exception.Should().BeAssignableTo<AzuriteServiceException>();
        ((AzuriteServiceException)exception).StatusCode.Should().Be(StatusCodes.Status416RangeNotSatisfiable);
    }

    #endregion

    #region AzuriteServiceException Tests

    [Fact(Timeout = 15000)]
    public void AzuriteServiceException_DefaultConstructor_ShouldSetStatusCode503()
    {
        // Arrange & Act
        var exception = new AzuriteServiceException();

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact(Timeout = 15000)]
    public void AzuriteServiceException_WithMessage_ShouldSetStatusCode503AndMessage()
    {
        // Arrange
        var message = "Azurite service unavailable";

        // Act
        var exception = new AzuriteServiceException(message);

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        exception.Message.Should().Be(message);
    }

    [Fact(Timeout = 15000)]
    public void AzuriteServiceException_StatusCode_ShouldBeSettable()
    {
        // Arrange
        var exception = new AzuriteServiceException("Test message");

        // Act
        exception.StatusCode = StatusCodes.Status502BadGateway;

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact(Timeout = 15000)]
    public void AzuriteServiceException_WithMessageAndInnerException_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Azurite service error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new AzuriteServiceException(message, innerException);

        // Assert
        exception.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    #endregion

    #region Inheritance Chain Tests

    [Fact(Timeout = 15000)]
    public void CatchingAzuriteServiceException_ShouldCatchAllDerivedExceptions()
    {
        // Arrange
        var exceptions = new Exception[]
        {
            new ResourceNotFoundException("Not found"),
            new ResourceExistsException("Already exists"),
            new RangeNotSatisfiableException("Range error"),
            new AzuriteServiceException("Service error")
        };

        // Act & Assert
        foreach (var exception in exceptions)
        {
            exception.Should().BeAssignableTo<AzuriteServiceException>();
            var azException = (AzuriteServiceException)exception;
            azException.StatusCode.Should().BeGreaterThan(0);
        }
    }

    [Fact(Timeout = 15000)]
    public void CatchingResourceNotFoundException_ShouldOnlyCatchResourceNotFoundException()
    {
        // Arrange
        var notFoundException = new ResourceNotFoundException("Not found");
        var existsException = new ResourceExistsException("Already exists");

        // Act & Assert
        notFoundException.Should().BeOfType<ResourceNotFoundException>();
        existsException.Should().NotBeOfType<ResourceNotFoundException>();
    }

    #endregion
}
