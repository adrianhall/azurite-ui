using Azure;
using Azure.Storage.Blobs.Models;
using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.UnitTests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace AzuriteUI.Web.UnitTests.Services.Azurite;

[ExcludeFromCodeCoverage]
public class AzuriteService_Tests
{
    private readonly FakeLogger<AzuriteService> _logger = new();

    #region Constructor Tests

    [Fact(Timeout = 15000)]
    public void Constructor_WithValidConnectionString_ShouldCreateInstance()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";

        // Act
        var service = new AzuriteService(connectionString, _logger);

        // Assert
        service.Should().NotBeNull();
        service.ConnectionString.Should().NotBeNullOrWhiteSpace();
        service.Logger.Should().BeSameAs(_logger);
        service.ServiceClient.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        string? connectionString = null;

        // Act
        Action act = () => new AzuriteService(connectionString!, _logger);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionString = "";

        // Act
        Action act = () => new AzuriteService(connectionString, _logger);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithWhitespaceConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionString = "   ";

        // Act
        Action act = () => new AzuriteService(connectionString, _logger);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";

        // Act
        Action act = () => new AzuriteService(connectionString, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithInvalidConnectionString_ShouldThrowException()
    {
        // Arrange
        var connectionString = "InvalidConnectionString";

        // Act
        Action act = () => new AzuriteService(connectionString, _logger);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithConfiguration_ShouldCreateInstance()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Azurite"] = connectionString
        });

        // Act
        var service = new AzuriteService(configuration, _logger);

        // Assert
        service.Should().NotBeNull();
        service.ConnectionString.Should().NotBeNullOrWhiteSpace();
        service.Logger.Should().BeSameAs(_logger);
        service.ServiceClient.Should().NotBeNull();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act
        IConfiguration configuration = null!;
        Action act = () => new AzuriteService(configuration, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(Timeout = 15000)]
    public void Constructor_WithConfigurationAndNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Azurite"] = connectionString
        });

        // Act
        Action act = () => new AzuriteService(configuration, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ConvertAzuriteException Tests

    [Fact(Timeout = 15000)]
    public void ConvertAzuriteException_With404Status_ShouldReturnResourceNotFoundException()
    {
        // Arrange
        var requestFailedException = new RequestFailedException(404, "Not found");
        var resourceName = "test-container";

        // Act
        var result = AzuriteService.ConvertAzuriteException(requestFailedException, resourceName);

        // Assert
        result.Should().BeOfType<ResourceNotFoundException>();
        var typedResult = (ResourceNotFoundException)result;
        typedResult.ResourceName.Should().Be(resourceName);
        typedResult.InnerException.Should().BeSameAs(requestFailedException);
    }

    [Fact(Timeout = 15000)]
    public void ConvertAzuriteException_With409Status_ShouldReturnResourceExistsException()
    {
        // Arrange
        var requestFailedException = new RequestFailedException(409, "Conflict");
        var resourceName = "test-container";

        // Act
        var result = AzuriteService.ConvertAzuriteException(requestFailedException, resourceName);

        // Assert
        result.Should().BeOfType<ResourceExistsException>();
        var typedResult = (ResourceExistsException)result;
        typedResult.ResourceName.Should().Be(resourceName);
        typedResult.InnerException.Should().BeSameAs(requestFailedException);
    }

    [Fact(Timeout = 15000)]
    public void ConvertAzuriteException_With416Status_ShouldReturnRangeNotSatisfiableException()
    {
        // Arrange
        var requestFailedException = new RequestFailedException(416, "Range Not Satisfiable");

        // Act
        var result = AzuriteService.ConvertAzuriteException(requestFailedException);

        // Assert
        result.Should().BeOfType<RangeNotSatisfiableException>();
        var typedResult = (RangeNotSatisfiableException)result;
        typedResult.InnerException.Should().BeSameAs(requestFailedException);
    }

    [Fact(Timeout = 15000)]
    public void ConvertAzuriteException_WithOtherStatus_ShouldReturnAzuriteServiceException()
    {
        // Arrange
        var requestFailedException = new RequestFailedException(500, "Internal Server Error");
        var resourceName = "test-container";

        // Act
        var result = AzuriteService.ConvertAzuriteException(requestFailedException, resourceName);

        // Assert
        result.Should().BeOfType<AzuriteServiceException>();
        var typedResult = (AzuriteServiceException)result;
        typedResult.InnerException.Should().BeSameAs(requestFailedException);
    }

    [Fact(Timeout = 15000)]
    public void ConvertAzuriteException_WithNullResourceName_ShouldReturnExceptionWithNullResourceName()
    {
        // Arrange
        var requestFailedException = new RequestFailedException(404, "Not found");

        // Act
        var result = AzuriteService.ConvertAzuriteException(requestFailedException, null);

        // Assert
        result.Should().BeOfType<ResourceNotFoundException>();
        var typedResult = (ResourceNotFoundException)result;
        typedResult.ResourceName.Should().BeNull();
    }

    #endregion

    #region ConvertToEncryptionScope Tests

    [Fact(Timeout = 15000)]
    public void ConvertToEncryptionScope_WithNullDefaultEncryptionScope_ShouldReturnNull()
    {
        // Arrange
        string? defaultEncryptionScope = null;
        bool? preventEncryptionScopeOverride = false;

        // Act
        var result = AzuriteService.ConvertToEncryptionScope(defaultEncryptionScope, preventEncryptionScopeOverride);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEncryptionScope_WithEmptyDefaultEncryptionScope_ShouldReturnNull()
    {
        // Arrange
        var defaultEncryptionScope = "";
        bool? preventEncryptionScopeOverride = false;

        // Act
        var result = AzuriteService.ConvertToEncryptionScope(defaultEncryptionScope, preventEncryptionScopeOverride);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEncryptionScope_WithValidDefaultEncryptionScope_ShouldReturnEncryptionScopeOptions()
    {
        // Arrange
        var defaultEncryptionScope = "test-scope";
        bool? preventEncryptionScopeOverride = true;

        // Act
        var result = AzuriteService.ConvertToEncryptionScope(defaultEncryptionScope, preventEncryptionScopeOverride);

        // Assert
        result.Should().NotBeNull();
        result!.DefaultEncryptionScope.Should().Be(defaultEncryptionScope);
        result.PreventEncryptionScopeOverride.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEncryptionScope_WithNullPreventEncryptionScopeOverride_ShouldDefaultToFalse()
    {
        // Arrange
        var defaultEncryptionScope = "test-scope";
        bool? preventEncryptionScopeOverride = null;

        // Act
        var result = AzuriteService.ConvertToEncryptionScope(defaultEncryptionScope, preventEncryptionScopeOverride);

        // Assert
        result.Should().NotBeNull();
        result!.PreventEncryptionScopeOverride.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEncryptionScope_WithFalsePreventEncryptionScopeOverride_ShouldSetToFalse()
    {
        // Arrange
        var defaultEncryptionScope = "test-scope";
        bool? preventEncryptionScopeOverride = false;

        // Act
        var result = AzuriteService.ConvertToEncryptionScope(defaultEncryptionScope, preventEncryptionScopeOverride);

        // Assert
        result.Should().NotBeNull();
        result!.PreventEncryptionScopeOverride.Should().BeFalse();
    }

    #endregion

    #region ConvertToPublicAccessType Tests

    [Fact(Timeout = 15000)]
    public void ConvertToPublicAccessType_WithNone_ShouldReturnPublicAccessTypeNone()
    {
        // Arrange
        var publicAccess = AzuritePublicAccess.None;

        // Act
        var result = AzuriteService.ConvertToPublicAccessType(publicAccess);

        // Assert
        result.Should().Be(PublicAccessType.None);
    }

    [Fact(Timeout = 15000)]
    public void ConvertToPublicAccessType_WithBlob_ShouldReturnPublicAccessTypeBlob()
    {
        // Arrange
        var publicAccess = AzuritePublicAccess.Blob;

        // Act
        var result = AzuriteService.ConvertToPublicAccessType(publicAccess);

        // Assert
        result.Should().Be(PublicAccessType.Blob);
    }

    [Fact(Timeout = 15000)]
    public void ConvertToPublicAccessType_WithContainer_ShouldReturnPublicAccessTypeBlobContainer()
    {
        // Arrange
        var publicAccess = AzuritePublicAccess.Container;

        // Act
        var result = AzuriteService.ConvertToPublicAccessType(publicAccess);

        // Assert
        result.Should().Be(PublicAccessType.BlobContainer);
    }

    [Fact(Timeout = 15000)]
    public void ConvertToPublicAccessType_WithNull_ShouldReturnPublicAccessTypeNone()
    {
        // Arrange
        AzuritePublicAccess? publicAccess = null;

        // Act
        var result = AzuriteService.ConvertToPublicAccessType(publicAccess);

        // Assert
        result.Should().Be(PublicAccessType.None);
    }

    #endregion

    #region HandleRequestFailedExceptionAsync<T> Tests

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_WithSuccessfulFunc_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "success";
        Func<Task<string>> func = () => Task.FromResult(expectedResult);

        // Act
        var result = await AzuriteService.HandleRequestFailedExceptionAsync("test-resource", func);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_WithRequestFailedExceptionStatus404_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var resourceName = "test-resource";
        Func<Task<string>> func = () => throw new RequestFailedException(404, "Not found");

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_WithRequestFailedExceptionStatus409_ShouldThrowResourceExistsException()
    {
        // Arrange
        var resourceName = "test-resource";
        Func<Task<string>> func = () => throw new RequestFailedException(409, "Conflict");

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<ResourceExistsException>();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_WithRequestFailedExceptionStatus416_ShouldThrowRangeNotSatisfiableException()
    {
        // Arrange
        var resourceName = "test-resource";
        Func<Task<string>> func = () => throw new RequestFailedException(416, "Range Not Satisfiable");

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<RangeNotSatisfiableException>();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_WithRequestFailedExceptionOtherStatus_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var resourceName = "test-resource";
        Func<Task<string>> func = () => throw new RequestFailedException(500, "Internal Server Error");

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_WithOtherException_ShouldPropagateException()
    {
        // Arrange
        var resourceName = "test-resource";
        var expectedException = new InvalidOperationException("Test exception");
        Func<Task<string>> func = () => throw expectedException;

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion

    #region HandleRequestFailedExceptionAsync (void) Tests

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_Void_WithSuccessfulFunc_ShouldComplete()
    {
        // Arrange
        var executed = false;
        Func<Task> func = () =>
        {
            executed = true;
            return Task.CompletedTask;
        };

        // Act
        await AzuriteService.HandleRequestFailedExceptionAsync("test-resource", func);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_Void_WithRequestFailedExceptionStatus404_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var resourceName = "test-resource";
        Func<Task> func = () => throw new RequestFailedException(404, "Not found");

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_Void_WithRequestFailedExceptionStatus409_ShouldThrowResourceExistsException()
    {
        // Arrange
        var resourceName = "test-resource";
        Func<Task> func = () => throw new RequestFailedException(409, "Conflict");

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<ResourceExistsException>();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_Void_WithRequestFailedExceptionStatus416_ShouldThrowRangeNotSatisfiableException()
    {
        // Arrange
        var resourceName = "test-resource";
        Func<Task> func = () => throw new RequestFailedException(416, "Range Not Satisfiable");

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<RangeNotSatisfiableException>();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_Void_WithRequestFailedExceptionOtherStatus_ShouldThrowAzuriteServiceException()
    {
        // Arrange
        var resourceName = "test-resource";
        Func<Task> func = () => throw new RequestFailedException(500, "Internal Server Error");

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<AzuriteServiceException>();
    }

    [Fact(Timeout = 15000)]
    public async Task HandleRequestFailedExceptionAsync_Void_WithOtherException_ShouldPropagateException()
    {
        // Arrange
        var resourceName = "test-resource";
        var expectedException = new InvalidOperationException("Test exception");
        Func<Task> func = () => throw expectedException;

        // Act
        Func<Task> act = async () => await AzuriteService.HandleRequestFailedExceptionAsync(resourceName, func);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion

    #region ParseHttpRange Tests

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithValidClosedRange_ShouldReturnHttpRange()
    {
        // Arrange
        var httpRange = "bytes=0-499";

        // Act
        var result = AzuriteService.ParseHttpRange(httpRange);

        // Assert
        result.Offset.Should().Be(0);
        result.Length.Should().Be(500);
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithValidOpenEndedRange_ShouldReturnHttpRangeWithNullLength()
    {
        // Arrange
        var httpRange = "bytes=500-";

        // Act
        var result = AzuriteService.ParseHttpRange(httpRange);

        // Assert
        result.Offset.Should().Be(500);
        result.Length.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithLargeRange_ShouldReturnCorrectHttpRange()
    {
        // Arrange
        var httpRange = "bytes=1000-2999";

        // Act
        var result = AzuriteService.ParseHttpRange(httpRange);

        // Assert
        result.Offset.Should().Be(1000);
        result.Length.Should().Be(2000);
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithSingleByteRange_ShouldReturnHttpRangeWithLengthOne()
    {
        // Arrange
        var httpRange = "bytes=0-0";

        // Act
        var result = AzuriteService.ParseHttpRange(httpRange);

        // Assert
        result.Offset.Should().Be(0);
        result.Length.Should().Be(1);
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange
        string? httpRange = null;

        // Act
        Action act = () => AzuriteService.ParseHttpRange(httpRange!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid Range header format*");
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var httpRange = "";

        // Act
        Action act = () => AzuriteService.ParseHttpRange(httpRange);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid Range header format*");
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithWhitespaceString_ShouldThrowArgumentException()
    {
        // Arrange
        var httpRange = "   ";

        // Act
        Action act = () => AzuriteService.ParseHttpRange(httpRange);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid Range header format*");
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithoutBytesPrefix_ShouldThrowArgumentException()
    {
        // Arrange
        var httpRange = "0-499";

        // Act
        Action act = () => AzuriteService.ParseHttpRange(httpRange);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid Range header format*");
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithInvalidFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var httpRange = "bytes=invalid";

        // Act
        Action act = () => AzuriteService.ParseHttpRange(httpRange);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid Range header format*");
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithSuffixRange_ShouldThrowArgumentException()
    {
        // Arrange
        var httpRange = "bytes=-500";

        // Act
        Action act = () => AzuriteService.ParseHttpRange(httpRange);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Suffix ranges*are not supported*");
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithMultipleRanges_ShouldThrowArgumentException()
    {
        // Arrange
        var httpRange = "bytes=0-499,500-999";

        // Act
        Action act = () => AzuriteService.ParseHttpRange(httpRange);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Only a single range is supported*");
    }

    [Fact(Timeout = 15000)]
    public void ParseHttpRange_WithCaseInsensitiveBytesPrefix_ShouldReturnHttpRange()
    {
        // Arrange
        var httpRange = "Bytes=0-499";

        // Act
        var result = AzuriteService.ParseHttpRange(httpRange);

        // Assert
        result.Offset.Should().Be(0);
        result.Length.Should().Be(500);
    }

    #endregion

    #region ValidateConnectionString Tests

    [Fact(Timeout = 15000)]
    public void ValidateConnectionString_WithValidConnectionString_ShouldReturnValidConnectionString()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";

        // Act
        var result = AzuriteService.ValidateConnectionString(connectionString);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("AccountName=devstoreaccount1");
    }

    [Fact(Timeout = 15000)]
    public void ValidateConnectionString_WithFullConnectionString_ShouldReturnFormattedConnectionString()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=http;AccountName=testaccount;AccountKey=dGVzdGtleQ==;BlobEndpoint=http://localhost:10000/testaccount";

        // Act
        var result = AzuriteService.ValidateConnectionString(connectionString);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("AccountName=testaccount");
        result.Should().Contain("AccountKey=dGVzdGtleQ==");
        result.Should().Contain("BlobEndpoint=http://localhost:10000/testaccount");
    }

    [Fact(Timeout = 15000)]
    public void ValidateConnectionString_WithInvalidConnectionString_ShouldThrowException()
    {
        // Arrange
        var connectionString = "InvalidConnectionString";

        // Act
        Action act = () => AzuriteService.ValidateConnectionString(connectionString);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact(Timeout = 15000)]
    public void ValidateConnectionString_WithEmptyConnectionString_ShouldThrowException()
    {
        // Arrange
        var connectionString = "";

        // Act
        Action act = () => AzuriteService.ValidateConnectionString(connectionString);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact(Timeout = 15000)]
    public void ValidateConnectionString_WithNullConnectionString_ShouldThrowException()
    {
        // Arrange
        string? connectionString = null;

        // Act
        Action act = () => AzuriteService.ValidateConnectionString(connectionString!);

        // Assert
        act.Should().Throw<Exception>();
    }

    #endregion

    #region GetHealthStatusAsync Tests

    [Fact(Timeout = 15000)]
    public async Task GetHealthStatusAsync_WithHealthyService_ShouldReturnHealthyStatus()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";
        
        var service = new TestableAzuriteService(connectionString, _logger, shouldThrow: false);

        // Act
        var result = await service.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeTrue();
        result.ConnectionString.Should().NotBeNullOrEmpty();
        result.ResponseTimeMilliseconds.Should().NotBeNull();
        result.ResponseTimeMilliseconds!.Value.Should().BeGreaterThanOrEqualTo(0);
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task GetHealthStatusAsync_WithUnhealthyService_ShouldReturnUnhealthyStatus()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";
        
        var exceptionMessage = "Service unavailable";
        var service = new TestableAzuriteService(connectionString, _logger, shouldThrow: true, exceptionMessage: exceptionMessage);

        // Act
        var result = await service.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeFalse();
        result.ConnectionString.Should().NotBeNullOrEmpty();
        result.ResponseTimeMilliseconds.Should().BeNull();
        result.ErrorMessage.Should().Be(exceptionMessage);
    }

    [Fact(Timeout = 15000)]
    public async Task GetHealthStatusAsync_WithException_ShouldLogError()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";
        
        var exceptionMessage = "Connection failed";
        var service = new TestableAzuriteService(connectionString, _logger, shouldThrow: true, exceptionMessage: exceptionMessage);

        // Act
        var result = await service.GetHealthStatusAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        _logger.Collector.GetSnapshot().Should().ContainSingle(log => log.Level == LogLevel.Error);
    }

    [Fact(Timeout = 15000)]
    public async Task GetHealthStatusAsync_WithRequestFailedException_ShouldCaptureErrorDetails()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";
        var service = new TestableAzuriteService(connectionString, _logger, shouldThrow: true, throwRequestFailed: true);

        // Act
        var result = await service.GetHealthStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsHealthy.Should().BeFalse();
        result.ConnectionString.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("Service request failed");
    }

    [Fact(Timeout = 15000)]
    public async Task GetHealthStatusAsync_WithException_ShouldNotSetResponseTime()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";
        var service = new TestableAzuriteService(connectionString, _logger, shouldThrow: true);

        // Act
        var result = await service.GetHealthStatusAsync();

        // Assert
        result.IsHealthy.Should().BeFalse();
        result.ResponseTimeMilliseconds.Should().BeNull();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test helper class that allows us to simulate failures without actual service calls.
    /// </summary>
    private class TestableAzuriteService : AzuriteService
    {
        private readonly bool _shouldThrow;
        private readonly string _exceptionMessage;
        private readonly bool _throwRequestFailed;

        public TestableAzuriteService(
            string connectionString,
            ILogger<AzuriteService> logger,
            bool shouldThrow,
            string exceptionMessage = "Test exception",
            bool throwRequestFailed = false)
            : base(connectionString, logger)
        {
            _shouldThrow = shouldThrow;
            _exceptionMessage = exceptionMessage;
            _throwRequestFailed = throwRequestFailed;
        }

        protected override Task CheckServiceIsAliveAsync(CancellationToken cancellationToken = default)
        {
            if (_shouldThrow)
            {
                if (_throwRequestFailed)
                {
                    throw new RequestFailedException(503, "Service request failed");
                }
                throw new InvalidOperationException(_exceptionMessage);
            }
            return Task.CompletedTask;
        }
    }

    #endregion
}
