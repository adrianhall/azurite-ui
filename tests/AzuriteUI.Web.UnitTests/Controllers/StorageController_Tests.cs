using AzuriteUI.Web.Controllers;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Repositories;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using NSubstitute;
using System.Text;

namespace AzuriteUI.Web.UnitTests.Controllers;

[ExcludeFromCodeCoverage]
public class StorageController_Tests
{
    #region Helpers
    /// <summary>
    /// Creates a test IEdmModel configured with ContainerDTO.
    /// </summary>
    private static IEdmModel CreateTestEdmModel()
    {
        var odataBuilder = new ODataConventionModelBuilder();
        odataBuilder.EnableLowerCamelCase();
        var containerEntity = odataBuilder.EntitySet<ContainerDTO>("Containers").EntityType;
        containerEntity.HasKey(c => c.Name);
        return odataBuilder.GetEdmModel();
    }

    /// <summary>
    /// Creates a configured HttpContext with the specified query string.
    /// </summary>
    private static DefaultHttpContext CreateTestHttpContext(string queryString = "")
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(queryString);
        return context;
    }

    /// <summary>
    /// Creates ODataQueryOptions of type T for testing with the specified query string.
    /// </summary>
    /// <typeparam name="T">The type for the query options.</typeparam>
    /// <param name="queryString">The query string to use.</param>
    /// <returns>ODataQueryOptions of type T.</returns>
    private static ODataQueryOptions<T> CreateTestQueryOptions<T>(string queryString = "") where T : class
    {
        var context = CreateTestHttpContext(queryString);
        var edmModel = CreateTestEdmModel();
        var queryContext = new ODataQueryContext(edmModel, typeof(T), new Microsoft.OData.UriParser.ODataPath());
        return new ODataQueryOptions<T>(queryContext, context.Request);
    }
    #endregion

    #region Constructor and Properties Tests

    [Fact(Timeout = 15000)]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();

        // Act
        var controller = new StorageController(repository, edmModel, logger);

        // Assert
        controller.Repository.Should().Be(repository);
        controller.EdmModel.Should().Be(edmModel);
        controller.Logger.Should().Be(logger);
    }

    #endregion

    #region CreateLink Tests

    [Fact(Timeout = 15000)]
    public void CreateLink_WithNoParameters_ShouldReturnEmptyString()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var result = StorageController.CreateLink(context.Request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void CreateLink_WithOnlySkipParameter_ShouldReturnQueryStringWithSkip()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var result = StorageController.CreateLink(context.Request, skip: 10);

        // Assert
        result.Should().Be("$skip=10");
    }

    [Fact(Timeout = 15000)]
    public void CreateLink_WithOnlyTopParameter_ShouldReturnQueryStringWithTop()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var result = StorageController.CreateLink(context.Request, top: 25);

        // Assert
        result.Should().Be("$top=25");
    }

    [Fact(Timeout = 15000)]
    public void CreateLink_WithBothSkipAndTopParameters_ShouldReturnQueryStringWithBoth()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var result = StorageController.CreateLink(context.Request, skip: 10, top: 25);

        // Assert
        result.Should().Be("$skip=10&$top=25");
    }

    [Fact(Timeout = 15000)]
    public void CreateLink_WithZeroSkipAndTop_ShouldReturnEmptyString()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var result = StorageController.CreateLink(context.Request, skip: 0, top: 0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void CreateLink_WithExistingQueryParameters_ShouldPreserveOtherParameters()
    {
        // Arrange
        var context = CreateTestHttpContext("?$filter=name eq 'test'&$orderby=name");

        // Act
        var result = StorageController.CreateLink(context.Request, skip: 10, top: 25);

        // Assert
        result.Should().Contain("$skip=10");
        result.Should().Contain("$top=25");
        result.Should().Contain("$filter=");
        result.Should().Contain("$orderby=name");
    }

    [Fact(Timeout = 15000)]
    public void CreateLink_WithExistingSkipAndTop_ShouldUpdateValues()
    {
        // Arrange
        var context = CreateTestHttpContext("?$skip=5&$top=10");

        // Act
        var result = StorageController.CreateLink(context.Request, skip: 15, top: 20);

        // Assert
        result.Should().Be("$skip=15&$top=20");
    }

    [Fact(Timeout = 15000)]
    public void CreateLink_WithZeroSkipButNonZeroTopAndExistingSkip_ShouldRemoveSkip()
    {
        // Arrange
        var context = CreateTestHttpContext("?$skip=10&$top=25");

        // Act
        var result = StorageController.CreateLink(context.Request, skip: 0, top: 25);

        // Assert
        result.Should().Be("$top=25");
        result.Should().NotContain("$skip");
    }

    [Fact(Timeout = 15000)]
    public void CreateLink_WithNonZeroSkipButZeroTopAndExistingTop_ShouldRemoveTop()
    {
        // Arrange
        var context = CreateTestHttpContext("?$skip=10&$top=25");

        // Act
        var result = StorageController.CreateLink(context.Request, skip: 10, top: 0);

        // Assert
        result.Should().Be("$skip=10");
        result.Should().NotContain("$top");
    }

    #endregion

    #region CalculateNextLinkParameters Tests

    [Fact(Timeout = 15000)]
    public void CalculateNextLinkParameters_WithMoreResultsAvailable_ShouldReturnNextPageParameters()
    {
        // Arrange
        int currentSkip = 0;
        int currentTop = 25;
        int resultCount = 25;
        int filteredCount = 100;

        // Act
        var result = StorageController.CalculateNextLinkParameters(currentSkip, currentTop, resultCount, filteredCount);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be((25, 25));
    }

    [Fact(Timeout = 15000)]
    public void CalculateNextLinkParameters_WithNoMoreResults_ShouldReturnNull()
    {
        // Arrange
        int currentSkip = 75;
        int currentTop = 25;
        int resultCount = 25;
        int filteredCount = 100;

        // Act
        var result = StorageController.CalculateNextLinkParameters(currentSkip, currentTop, resultCount, filteredCount);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void CalculateNextLinkParameters_WithPartialLastPage_ShouldReturnNull()
    {
        // Arrange
        int currentSkip = 90;
        int currentTop = 25;
        int resultCount = 10;
        int filteredCount = 100;

        // Act
        var result = StorageController.CalculateNextLinkParameters(currentSkip, currentTop, resultCount, filteredCount);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void CalculateNextLinkParameters_WithFirstPage_ShouldCalculateCorrectNextSkip()
    {
        // Arrange
        int currentSkip = 0;
        int currentTop = 50;
        int resultCount = 50;
        int filteredCount = 150;

        // Act
        var result = StorageController.CalculateNextLinkParameters(currentSkip, currentTop, resultCount, filteredCount);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be((50, 50));
    }

    [Fact(Timeout = 15000)]
    public void CalculateNextLinkParameters_WithZeroResults_ShouldReturnNull()
    {
        // Arrange
        int currentSkip = 0;
        int currentTop = 25;
        int resultCount = 0;
        int filteredCount = 0;

        // Act
        var result = StorageController.CalculateNextLinkParameters(currentSkip, currentTop, resultCount, filteredCount);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void CalculateNextLinkParameters_WithExactlyOnePageOfResults_ShouldReturnNull()
    {
        // Arrange
        int currentSkip = 0;
        int currentTop = 25;
        int resultCount = 25;
        int filteredCount = 25;

        // Act
        var result = StorageController.CalculateNextLinkParameters(currentSkip, currentTop, resultCount, filteredCount);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CalculatePrevLinkParameters Tests

    [Fact(Timeout = 15000)]
    public void CalculatePrevLinkParameters_WithSkipGreaterThanZero_ShouldReturnPreviousPageParameters()
    {
        // Arrange
        int currentSkip = 25;
        int currentTop = 25;

        // Act
        var result = StorageController.CalculatePrevLinkParameters(currentSkip, currentTop);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be((0, 25));
    }

    [Fact(Timeout = 15000)]
    public void CalculatePrevLinkParameters_WithSkipZero_ShouldReturnNull()
    {
        // Arrange
        int currentSkip = 0;
        int currentTop = 25;

        // Act
        var result = StorageController.CalculatePrevLinkParameters(currentSkip, currentTop);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void CalculatePrevLinkParameters_WithSkipLessThanTop_ShouldReturnZeroSkip()
    {
        // Arrange
        int currentSkip = 10;
        int currentTop = 25;

        // Act
        var result = StorageController.CalculatePrevLinkParameters(currentSkip, currentTop);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be((0, 25));
    }

    [Fact(Timeout = 15000)]
    public void CalculatePrevLinkParameters_WithSkipEqualToTop_ShouldReturnZeroSkip()
    {
        // Arrange
        int currentSkip = 25;
        int currentTop = 25;

        // Act
        var result = StorageController.CalculatePrevLinkParameters(currentSkip, currentTop);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be((0, 25));
    }

    [Fact(Timeout = 15000)]
    public void CalculatePrevLinkParameters_WithLargeSkip_ShouldCalculateCorrectPreviousSkip()
    {
        // Arrange
        int currentSkip = 100;
        int currentTop = 25;

        // Act
        var result = StorageController.CalculatePrevLinkParameters(currentSkip, currentTop);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be((75, 25));
    }

    [Fact(Timeout = 15000)]
    public void CalculatePrevLinkParameters_WithDifferentTopSize_ShouldUseCurrentTop()
    {
        // Arrange
        int currentSkip = 50;
        int currentTop = 50;

        // Act
        var result = StorageController.CalculatePrevLinkParameters(currentSkip, currentTop);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be((0, 50));
    }

    #endregion

    #region BuildServiceProvider Tests

    [Fact(Timeout = 15000)]
    public void BuildServiceProvider_WithValidRequest_ShouldReturnServiceProvider()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var result = StorageController.BuildServiceProvider(context.Request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IServiceProvider>();
    }

    [Fact(Timeout = 15000)]
    public void BuildServiceProvider_ShouldSetODataFeatureServices()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var result = StorageController.BuildServiceProvider(context.Request);

        // Assert
        context.Request.ODataFeature().Services.Should().NotBeNull();
        context.Request.ODataFeature().Services.Should().Be(result);
    }

    [Fact(Timeout = 15000)]
    public void BuildServiceProvider_ShouldRegisterDefaultQueryConfigurations()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var provider = StorageController.BuildServiceProvider(context.Request);

        // Assert
        var config = provider.GetService(typeof(DefaultQueryConfigurations));
        config.Should().NotBeNull();
        config.Should().BeOfType<DefaultQueryConfigurations>();
    }

    [Fact(Timeout = 15000)]
    public void BuildServiceProvider_ShouldRegisterODataQuerySettings()
    {
        // Arrange
        var context = CreateTestHttpContext();

        // Act
        var provider = StorageController.BuildServiceProvider(context.Request);

        // Assert
        var settings = provider.GetService(typeof(ODataQuerySettings));
        settings.Should().NotBeNull();
        settings.Should().BeOfType<ODataQuerySettings>();
    }

    #endregion

    #region CreatePagedResponse Tests

    [Fact(Timeout = 15000)]
    public void CreatePagedResponse_WithValidParameters_ShouldCreateResponse()
    {
        // Arrange
        var queryOptions = CreateTestQueryOptions<ContainerDTO>();
        var results = new List<object> { new { Id = 1 }, new { Id = 2 } };
        int totalCount = 100;
        int filteredCount = 50;

        // Act
        var response = StorageController.CreatePagedResponse(queryOptions, results, totalCount, filteredCount);

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(100);
        response.FilteredCount.Should().Be(50);
    }

    [Fact(Timeout = 15000)]
    public void CreatePagedResponse_WithNullResults_ShouldReturnEmptyItems()
    {
        // Arrange
        var queryOptions = CreateTestQueryOptions<ContainerDTO>();
        int totalCount = 0;
        int filteredCount = 0;

        // Act
        var response = StorageController.CreatePagedResponse(queryOptions, null, totalCount, filteredCount);

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.FilteredCount.Should().Be(0);
    }

    [Fact(Timeout = 15000)]
    public void CreatePagedResponse_WithMoreResultsAvailable_ShouldHaveNextLink()
    {
        // Arrange
        var queryOptions = CreateTestQueryOptions<ContainerDTO>("?$skip=0&$top=25");
        var results = Enumerable.Range(0, 25).Select(i => new { Id = i }).Cast<object>();
        int totalCount = 100;
        int filteredCount = 100;

        // Act
        var response = StorageController.CreatePagedResponse(queryOptions, results, totalCount, filteredCount);

        // Assert
        response.NextLink.Should().NotBeNull();
        response.NextLink.Should().Contain("$skip=25");
    }

    [Fact(Timeout = 15000)]
    public void CreatePagedResponse_WithNoMoreResults_ShouldNotHaveNextLink()
    {
        // Arrange
        var queryOptions = CreateTestQueryOptions<ContainerDTO>("?$skip=75&$top=25");
        var results = Enumerable.Range(0, 25).Select(i => new { Id = i }).Cast<object>();
        int totalCount = 100;
        int filteredCount = 100;

        // Act
        var response = StorageController.CreatePagedResponse(queryOptions, results, totalCount, filteredCount);

        // Assert
        response.NextLink.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void CreatePagedResponse_WithSkipGreaterThanZero_ShouldHavePrevLink()
    {
        // Arrange
        var queryOptions = CreateTestQueryOptions<ContainerDTO>("?$skip=25&$top=25");
        var results = Enumerable.Range(0, 25).Select(i => new { Id = i }).Cast<object>();
        int totalCount = 100;
        int filteredCount = 100;

        // Act
        var response = StorageController.CreatePagedResponse(queryOptions, results, totalCount, filteredCount);

        // Assert
        response.PrevLink.Should().NotBeNull();
        response.PrevLink.Should().NotContain("$skip");
        response.PrevLink.Should().Contain("$top=25");
    }

    [Fact(Timeout = 15000)]
    public void CreatePagedResponse_WithSkipZero_ShouldNotHavePrevLink()
    {
        // Arrange
        var queryOptions = CreateTestQueryOptions<ContainerDTO>("?$skip=0&$top=25");
        var results = Enumerable.Range(0, 25).Select(i => new { Id = i }).Cast<object>();
        int totalCount = 100;
        int filteredCount = 100;

        // Act
        var response = StorageController.CreatePagedResponse(queryOptions, results, totalCount, filteredCount);

        // Assert
        response.PrevLink.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void CreatePagedResponse_WithEmptyResults_ShouldHaveZeroCounts()
    {
        // Arrange
        var queryOptions = CreateTestQueryOptions<ContainerDTO>();
        var results = new List<object>();
        int totalCount = 0;
        int filteredCount = 0;

        // Act
        var response = StorageController.CreatePagedResponse(queryOptions, results, totalCount, filteredCount);

        // Assert
        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.FilteredCount.Should().Be(0);
        response.NextLink.Should().BeNull();
        response.PrevLink.Should().BeNull();
    }

    #endregion

    #region GetResponseForConditionalRequest Tests

    /// <summary>
    /// Creates a StorageController with the specified request method and headers.
    /// </summary>
    private static StorageController CreateControllerWithRequest(
        string method = "GET",
        Action<HttpRequest>? configureHeaders = null)
    {
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();

        var controller = new StorageController(repository, edmModel, logger);
        var context = new DefaultHttpContext();
        context.Request.Method = method;

        configureHeaders?.Invoke(context.Request);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        return controller;
    }

    /// <summary>
    /// Creates a test entity with the specified ETag and LastModified values.
    /// </summary>
    private static ContainerDTO CreateTestEntity(string etag = "\"test-etag\"", DateTimeOffset? lastModified = null)
    {
        return new ContainerDTO
        {
            Name = "test-container",
            ETag = etag,
            LastModified = lastModified ?? DateTimeOffset.UtcNow
        };
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithNoConditionalHeaders_ShouldReturnNull()
    {
        // Arrange
        var controller = CreateControllerWithRequest();
        var entity = CreateTestEntity();

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithMatchingIfMatch_ShouldReturnNull()
    {
        // Arrange
        var etag = "\"test-etag\"";
        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfMatch] = etag;
        });
        var entity = CreateTestEntity(etag);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithNonMatchingIfMatch_ShouldReturn412()
    {
        // Arrange
        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfMatch] = "\"different-etag\"";
        });
        var entity = CreateTestEntity("\"test-etag\"");

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status412PreconditionFailed);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithIfUnmodifiedSinceBeforeLastModified_ShouldReturn412()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;
        var ifUnmodifiedSince = lastModified.AddHours(-1);

        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfUnmodifiedSince] = ifUnmodifiedSince.ToString("R");
        });
        var entity = CreateTestEntity(lastModified: lastModified);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status412PreconditionFailed);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithIfUnmodifiedSinceAfterLastModified_ShouldReturnNull()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;
        var ifUnmodifiedSince = lastModified.AddHours(1);

        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfUnmodifiedSince] = ifUnmodifiedSince.ToString("R");
        });
        var entity = CreateTestEntity(lastModified: lastModified);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithIfUnmodifiedSinceEqualToLastModified_ShouldReturn412()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;

        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfUnmodifiedSince] = lastModified.ToString("R");
        });
        var entity = CreateTestEntity(lastModified: lastModified);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status412PreconditionFailed);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithMatchingIfNoneMatchOnGetRequest_ShouldReturn304()
    {
        // Arrange
        var etag = "\"test-etag\"";
        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfNoneMatch] = etag;
        });
        var entity = CreateTestEntity(etag);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithMatchingIfNoneMatchOnHeadRequest_ShouldReturn304()
    {
        // Arrange
        var etag = "\"test-etag\"";
        var controller = CreateControllerWithRequest("HEAD", request =>
        {
            request.Headers[HeaderNames.IfNoneMatch] = etag;
        });
        var entity = CreateTestEntity(etag);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithMatchingIfNoneMatchOnPutRequest_ShouldReturn412()
    {
        // Arrange
        var etag = "\"test-etag\"";
        var controller = CreateControllerWithRequest("PUT", request =>
        {
            request.Headers[HeaderNames.IfNoneMatch] = etag;
        });
        var entity = CreateTestEntity(etag);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status412PreconditionFailed);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithNonMatchingIfNoneMatch_ShouldReturnNull()
    {
        // Arrange
        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfNoneMatch] = "\"different-etag\"";
        });
        var entity = CreateTestEntity("\"test-etag\"");

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithIfModifiedSinceAfterLastModifiedOnGetRequest_ShouldReturn304()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;
        var ifModifiedSince = lastModified.AddHours(1);

        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfModifiedSince] = ifModifiedSince.ToString("R");
        });
        var entity = CreateTestEntity(lastModified: lastModified);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithIfModifiedSinceAfterLastModifiedOnPutRequest_ShouldReturn412()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;
        var ifModifiedSince = lastModified.AddHours(1);

        var controller = CreateControllerWithRequest("PUT", request =>
        {
            request.Headers[HeaderNames.IfModifiedSince] = ifModifiedSince.ToString("R");
        });
        var entity = CreateTestEntity(lastModified: lastModified);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status412PreconditionFailed);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithIfModifiedSinceBeforeLastModified_ShouldReturnNull()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;
        var ifModifiedSince = lastModified.AddHours(-1);

        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfModifiedSince] = ifModifiedSince.ToString("R");
        });
        var entity = CreateTestEntity(lastModified: lastModified);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithIfMatchTakesPrecedenceOverIfUnmodifiedSince_ShouldUseIfMatch()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;
        var ifUnmodifiedSince = lastModified.AddHours(-1); // Would fail if checked

        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfMatch] = "\"test-etag\""; // Matching
            request.Headers[HeaderNames.IfUnmodifiedSince] = ifUnmodifiedSince.ToString("R");
        });
        var entity = CreateTestEntity("\"test-etag\"", lastModified);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().BeNull(); // IfMatch matches, so precondition passes
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithIfNoneMatchTakesPrecedenceOverIfModifiedSince_ShouldUseIfNoneMatch()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;
        var ifModifiedSince = lastModified.AddHours(1); // Would return 304 if checked

        var controller = CreateControllerWithRequest("GET", request =>
        {
            request.Headers[HeaderNames.IfNoneMatch] = "\"different-etag\""; // Non-matching
            request.Headers[HeaderNames.IfModifiedSince] = ifModifiedSince.ToString("R");
        });
        var entity = CreateTestEntity("\"test-etag\"", lastModified);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().BeNull(); // IfNoneMatch doesn't match, so precondition passes
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithCaseInsensitiveGetMethod_ShouldTreatAsFetch()
    {
        // Arrange
        var etag = "\"test-etag\"";
        var controller = CreateControllerWithRequest("get", request =>
        {
            request.Headers[HeaderNames.IfNoneMatch] = etag;
        });
        var entity = CreateTestEntity(etag);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact(Timeout = 15000)]
    public void GetResponseForConditionalRequest_WithCaseInsensitiveHeadMethod_ShouldTreatAsFetch()
    {
        // Arrange
        var etag = "\"test-etag\"";
        var controller = CreateControllerWithRequest("head", request =>
        {
            request.Headers[HeaderNames.IfNoneMatch] = etag;
        });
        var entity = CreateTestEntity(etag);

        // Act
        var result = controller.GetResponseForConditionalRequest(entity);

        // Assert
        result.Should().Be(StatusCodes.Status304NotModified);
    }

    #endregion

    #region ConditionalResponse Tests

    [Fact(Timeout = 15000)]
    public void ConditionalResponse_With304StatusCode_ShouldReturnStatusCodeResult()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);
        var entity = CreateTestEntity();

        // Act
        var result = controller.ConditionalResponse(StatusCodes.Status304NotModified, entity);

        // Assert
        result.Should().BeOfType<StatusCodeResult>();
        var statusCodeResult = result as StatusCodeResult;
        statusCodeResult!.StatusCode.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact(Timeout = 15000)]
    public void ConditionalResponse_With412StatusCode_ShouldReturnObjectResult()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);
        var entity = CreateTestEntity();

        // Act
        var result = controller.ConditionalResponse(StatusCodes.Status412PreconditionFailed, entity);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status412PreconditionFailed);
        objectResult.Value.Should().Be(entity);
    }

    [Fact(Timeout = 15000)]
    public void ConditionalResponse_WithInvalidStatusCode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);
        var entity = CreateTestEntity();

        // Act
        Action act = () => controller.ConditionalResponse(StatusCodes.Status200OK, entity);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid status code for conditional response.");
    }

    [Fact(Timeout = 15000)]
    public void ConditionalResponse_With400StatusCode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);
        var entity = CreateTestEntity();

        // Act
        Action act = () => controller.ConditionalResponse(StatusCodes.Status400BadRequest, entity);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact(Timeout = 15000)]
    public void ConditionalResponse_With500StatusCode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);
        var entity = CreateTestEntity();

        // Act
        Action act = () => controller.ConditionalResponse(StatusCodes.Status500InternalServerError, entity);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region DownloadBlobAsync Tests

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithValidParameters_ShouldReturnFileStreamResult()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 12,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status200OK
        };

        repository.DownloadBlobAsync(containerName, blobName, null, Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileResult = result as FileStreamResult;
        fileResult!.FileStream.Should().BeSameAs(content);
        fileResult.ContentType.Should().Be("text/plain");
        fileResult.EntityTag.Should().NotBeNull();
        fileResult.EntityTag!.Tag.ToString().Should().Contain("test-etag");
        fileResult.LastModified.Should().NotBeNull();
        fileResult.EnableRangeProcessing.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithDispositionAttachment_ShouldSetContentDispositionHeader()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 12,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status200OK
        };

        repository.DownloadBlobAsync(containerName, blobName, null, Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName, disposition: "attachment");

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        context.Response.Headers.ContentDisposition.Should().NotBeEmpty();
        var dispositionHeader = context.Response.Headers.ContentDisposition.ToString();
        dispositionHeader.Should().Contain("attachment");
        dispositionHeader.Should().Contain(blobName);
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithDispositionInline_ShouldSetContentDispositionHeader()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 12,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status200OK
        };

        repository.DownloadBlobAsync(containerName, blobName, null, Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName, disposition: "inline");

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        context.Response.Headers.ContentDisposition.Should().NotBeEmpty();
        var dispositionHeader = context.Response.Headers.ContentDisposition.ToString();
        dispositionHeader.Should().Contain("inline");
        dispositionHeader.Should().Contain(blobName);
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithoutDisposition_ShouldNotSetContentDispositionHeader()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 12,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status200OK
        };

        repository.DownloadBlobAsync(containerName, blobName, null, Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        context.Response.Headers.ContentDisposition.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithCaseInsensitiveDisposition_ShouldAccept()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 12,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status200OK
        };

        repository.DownloadBlobAsync(containerName, blobName, null, Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName, disposition: "ATTACHMENT");

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        context.Response.Headers.ContentDisposition.Should().NotBeEmpty();
        var dispositionHeader = context.Response.Headers.ContentDisposition.ToString();
        dispositionHeader.Should().Contain("attachment");
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithRangeRequest_ShouldReturn206AndSetContentRangeHeader()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderNames.Range] = "bytes=0-9";
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("0123456789"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 10,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status206PartialContent,
            ContentRange = "bytes 0-9/20"
        };

        repository.DownloadBlobAsync(containerName, blobName, "bytes=0-9", Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        context.Response.StatusCode.Should().Be(StatusCodes.Status206PartialContent);
        context.Response.Headers.ContentRange.Should().NotBeEmpty();
        context.Response.Headers.ContentRange.ToString().Should().Be("bytes 0-9/20");
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithoutRangeRequest_ShouldReturn200AndNoContentRangeHeader()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 12,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status200OK
        };

        repository.DownloadBlobAsync(containerName, blobName, null, Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Headers.ContentRange.Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_ShouldSetAcceptRangesHeader()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 12,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status200OK
        };

        repository.DownloadBlobAsync(containerName, blobName, null, Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        context.Response.Headers.AcceptRanges.Should().NotBeEmpty();
        context.Response.Headers.AcceptRanges.ToString().Should().Be("bytes");
    }

    [Fact(Timeout = 15000)]
    public async Task DownloadBlobAsync_WithSpecialCharactersInFilename_ShouldEncodeContentDisposition()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        var context = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var containerName = "test-container";
        var blobName = "test file with spaces.txt";
        var content = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var downloadResult = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = content,
            ContentType = "text/plain",
            ContentLength = 12,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status200OK
        };

        repository.DownloadBlobAsync(containerName, blobName, null, Arg.Any<CancellationToken>())
            .Returns(downloadResult);

        // Act
        var result = await controller.DownloadBlobAsync(containerName, blobName, disposition: "attachment");

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        context.Response.Headers.ContentDisposition.Should().NotBeEmpty();
        var dispositionHeader = context.Response.Headers.ContentDisposition.ToString();
        dispositionHeader.Should().Contain("attachment");
        // The filename should be URI-encoded for special characters
        (dispositionHeader.Contains("test%20file%20with%20spaces.txt") || dispositionHeader.Contains(blobName)).Should().BeTrue();
    }

    #endregion

    #region ConvertToEntityTagHeaderValue Tests

    [Fact(Timeout = 15000)]
    public void ConvertToEntityTagHeaderValue_WithNullETag_ShouldReturnNull()
    {
        // Arrange
        string? etag = null;

        // Act
        var result = StorageController.ConvertToEntityTagHeaderValue(etag);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEntityTagHeaderValue_WithEmptyETag_ShouldReturnNull()
    {
        // Arrange
        string etag = string.Empty;

        // Act
        var result = StorageController.ConvertToEntityTagHeaderValue(etag);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEntityTagHeaderValue_WithQuotedETag_ShouldReturnEntityTag()
    {
        // Arrange
        string etag = "\"test-etag\"";

        // Act
        var result = StorageController.ConvertToEntityTagHeaderValue(etag);

        // Assert
        result.Should().NotBeNull();
        result!.Tag.ToString().Should().Be("\"test-etag\"");
        result.IsWeak.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEntityTagHeaderValue_WithUnquotedETag_ShouldAddQuotesAndReturnEntityTag()
    {
        // Arrange
        string etag = "test-etag";

        // Act
        var result = StorageController.ConvertToEntityTagHeaderValue(etag);

        // Assert
        result.Should().NotBeNull();
        result!.Tag.ToString().Should().Be("\"test-etag\"");
        result.IsWeak.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEntityTagHeaderValue_WithComplexETag_ShouldHandleCorrectly()
    {
        // Arrange
        string etag = "\"0x8D9F8B8C9D0E1F2\"";

        // Act
        var result = StorageController.ConvertToEntityTagHeaderValue(etag);

        // Assert
        result.Should().NotBeNull();
        result!.Tag.ToString().Should().Be("\"0x8D9F8B8C9D0E1F2\"");
    }

    [Fact(Timeout = 15000)]
    public void ConvertToEntityTagHeaderValue_WithETagContainingSpecialCharacters_ShouldPreserve()
    {
        // Arrange
        string etag = "abc123-xyz_456";

        // Act
        var result = StorageController.ConvertToEntityTagHeaderValue(etag);

        // Assert
        result.Should().NotBeNull();
        result!.Tag.ToString().Should().Be("\"abc123-xyz_456\"");
    }

    #endregion

    #region DeleteContainerAsync Tests

    [Fact(Timeout = 15000)]
    public async Task DeleteContainerAsync_WhenAzuriteServiceExceptionWith404IsThrown_ShouldReturnNoContent()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        // Set up HttpContext
        var context = new DefaultHttpContext();
        context.Request.Method = "DELETE";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        var containerName = "test-container";
        var container = new ContainerDTO
        {
            Name = containerName,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow
        };

        // Mock repository to return a container
        repository.GetContainerAsync(containerName, Arg.Any<CancellationToken>())
            .Returns(container);

        // Mock repository to throw AzuriteServiceException with 404 when deleting
        var exception = new AzuriteServiceException("Container not found in Azurite")
        {
            StatusCode = StatusCodes.Status404NotFound
        };
        repository.DeleteContainerAsync(containerName, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));

        // Act
        var result = await controller.DeleteContainerAsync(containerName);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region DeleteBlobAsync Tests

    [Fact(Timeout = 15000)]
    public async Task DeleteBlobAsync_WhenAzuriteServiceExceptionWith404IsThrown_ShouldReturnNoContent()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var edmModel = Substitute.For<IEdmModel>();
        var logger = Substitute.For<ILogger<StorageController>>();
        var controller = new StorageController(repository, edmModel, logger);

        // Set up HttpContext
        var context = new DefaultHttpContext();
        context.Request.Method = "DELETE";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        var containerName = "test-container";
        var blobName = "test-blob.txt";
        var blob = new BlobDTO
        {
            Name = blobName,
            ContainerName = containerName,
            ETag = "\"test-etag\"",
            LastModified = DateTimeOffset.UtcNow
        };

        // Mock repository to return a blob
        repository.GetBlobAsync(containerName, blobName, Arg.Any<CancellationToken>())
            .Returns(blob);

        // Mock repository to throw AzuriteServiceException with 404 when deleting
        var exception = new AzuriteServiceException("Blob not found in Azurite")
        {
            StatusCode = StatusCodes.Status404NotFound
        };
        repository.DeleteBlobAsync(containerName, blobName, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));

        // Act
        var result = await controller.DeleteBlobAsync(containerName, blobName);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion
}
