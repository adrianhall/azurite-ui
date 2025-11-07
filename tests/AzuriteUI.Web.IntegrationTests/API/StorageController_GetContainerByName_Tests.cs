using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_GetContainerByName_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Basic GET Tests

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithExistingContainer_ShouldReturnContainer()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}");
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
        result.ETag.Should().NotBeNullOrEmpty();
        result.LastModified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        result.BlobCount.Should().Be(0);
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithContainerContainingBlobs_ShouldIncludeBlobCount()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob3.txt", "content3");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}");
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
        result.BlobCount.Should().Be(3);
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithContainerWithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container", metadata);
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}");
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Metadata.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        result.Metadata.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithNonExistentContainer_ShouldReturn404()
    {
        // Arrange
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();
        var nonExistentContainer = "container-that-does-not-exist-12345";

        // Act
        var response = await client.GetAsync($"/api/containers/{nonExistentContainer}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithInvalidContainerName_ShouldReturn404()
    {
        // Arrange
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act - Use a clearly invalid container name
        var response = await client.GetAsync("/api/containers/invalid-container-!@#$");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Conditional Request Tests - If-Match

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithMatchingIfMatch_ShouldReturn200()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its ETag
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var etag = EnsureQuotedETag(container!.ETag);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfMatch, etag);
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithNonMatchingIfMatch_ShouldReturn412()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfMatch, "\"non-matching-etag\"");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    #endregion

    #region Conditional Request Tests - If-None-Match

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithMatchingIfNoneMatch_ShouldReturn304()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its ETag
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var etag = EnsureQuotedETag(container!.ETag);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfNoneMatch, etag);
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithNonMatchingIfNoneMatch_ShouldReturn200()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfNoneMatch, "\"different-etag\"");
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
    }

    #endregion

    #region Conditional Request Tests - If-Modified-Since

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithIfModifiedSinceBeforeLastModified_ShouldReturn200()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var lastModified = container!.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfModifiedSince, lastModified.AddHours(-1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithIfModifiedSinceAfterLastModified_ShouldReturn304()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var lastModified = container!.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfModifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    #endregion

    #region Conditional Request Tests - If-Unmodified-Since

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithIfUnmodifiedSinceAfterLastModified_ShouldReturn200()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var lastModified = container!.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfUnmodifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithIfUnmodifiedSinceBeforeLastModified_ShouldReturn412()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var lastModified = container!.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfUnmodifiedSince, lastModified.AddHours(-1).ToString("R"));
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    #endregion

    #region Conditional Request Tests - Combined Headers

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithMatchingIfMatchAndIfUnmodifiedSince_ShouldReturn200()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its ETag and LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var etag = EnsureQuotedETag(container!.ETag);
        var lastModified = container.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfMatch, etag);
        request.Headers.Add(HeaderNames.IfUnmodifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithMatchingIfNoneMatchAndIfModifiedSince_ShouldReturn304()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its ETag and LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var etag = EnsureQuotedETag(container!.ETag);
        var lastModified = container.LastModified;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfNoneMatch, etag);
        request.Headers.Add(HeaderNames.IfModifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithIfMatchTakesPrecedenceOverIfUnmodifiedSince_ShouldReturn200()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its ETag and LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var etag = EnsureQuotedETag(container!.ETag);
        var lastModified = container.LastModified;

        // Act - If-Match matches, so should succeed even though If-Unmodified-Since would fail
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfMatch, etag);
        request.Headers.Add(HeaderNames.IfUnmodifiedSince, lastModified.AddHours(-1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task GetContainerByName_WithIfNoneMatchTakesPrecedenceOverIfModifiedSince_ShouldReturn200()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using HttpClient client = Fixture.CreateClient();

        // Get the container first to obtain its LastModified
        var initialResponse = await client.GetAsync($"/api/containers/{containerName}");
        var container = await initialResponse.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);
        var lastModified = container!.LastModified;

        // Act - If-None-Match doesn't match, so should succeed even though If-Modified-Since would return 304
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}");
        request.Headers.Add(HeaderNames.IfNoneMatch, "\"different-etag\"");
        request.Headers.Add(HeaderNames.IfModifiedSince, lastModified.AddHours(1).ToString("R"));
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ContainerDTO>(ServiceFixture.JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
    }

    #endregion
}
