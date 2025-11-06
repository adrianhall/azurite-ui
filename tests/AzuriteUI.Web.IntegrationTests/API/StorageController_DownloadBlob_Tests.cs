using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class StorageController_DownloadBlob_Tests(ServiceFixture fixture) : BaseApiTest()
{
    #region Basic Download Tests

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithExistingBlob_ShouldReturn200AndContent()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "This is test content for the blob.";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be(blobContent);

        // Verify headers
        response.Headers.Should().ContainKey("Accept-Ranges");
        response.Headers.GetValues("Accept-Ranges").Should().Contain("bytes");
        response.Headers.ETag.Should().NotBeNull();
        response.Content.Headers.LastModified.Should().NotBeNull();
    }

    [Theory(Timeout = 60000)]
    [InlineData("attachment")]
    [InlineData("inline")]
    public async Task DownloadBlob_WithDispositionQueryParam_ShouldSetContentDispositionHeader(string disposition)
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "Test content";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}/content?disposition={disposition}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be(disposition);
        response.Content.Headers.ContentDisposition.FileName.Should().Be(blobName);
    }

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithoutDisposition_ShouldNotSetContentDispositionHeader()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "Test content";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition.Should().BeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithDispositionCaseInsensitive_ShouldAccept()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "Test content";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}/content?disposition=ATTACHMENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
    }

    #endregion

    #region Range Request Tests

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithValidRangeHeader_ShouldReturn206PartialContent()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "0123456789ABCDEFGHIJ";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}/content");
        request.Headers.Add(HeaderNames.Range, "bytes=0-9");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Content.Headers.ContentRange.Should().NotBeNull();
        response.Content.Headers.ContentRange!.ToString().Should().Contain("bytes 0-9");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("0123456789");
    }

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithRangeFromMiddle_ShouldReturn206WithCorrectContent()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "0123456789ABCDEFGHIJ";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}/content");
        request.Headers.Add(HeaderNames.Range, "bytes=10-14");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Content.Headers.ContentRange.Should().NotBeNull();

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("ABCDE");
    }

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithRangeToEnd_ShouldReturn206WithCorrectContent()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "0123456789ABCDEFGHIJ";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}/content");
        request.Headers.Add(HeaderNames.Range, "bytes=15-");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("FGHIJ");
    }

    #endregion

    #region Validation Tests

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithInvalidDisposition_ShouldReturn400()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "Test content";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{blobName}/content?disposition=invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Error Tests

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithNonExistentBlob_ShouldReturn404WithProblemDetails()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var nonExistentBlob = "blob-that-does-not-exist-12345.txt";

        // Act
        var response = await client.GetAsync($"/api/containers/{containerName}/blobs/{nonExistentBlob}/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        root.GetProperty("title").GetString().Should().Be("Not Found");
        root.GetProperty("detail").GetString().Should().Contain("not found");
    }

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithNonExistentContainer_ShouldReturn404WithProblemDetails()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();
        var nonExistentContainer = "container-that-does-not-exist-12345";

        // Act
        var response = await client.GetAsync($"/api/containers/{nonExistentContainer}/blobs/test-blob.txt/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        root.GetProperty("title").GetString().Should().Be("Not Found");
    }

    [Fact(Timeout = 60000)]
    public async Task DownloadBlob_WithInvalidRangeHeader_ShouldReturn416WithProblemDetails()
    {
        // Arrange
        await fixture.Azurite.CleanupAsync();
        var containerName = await fixture.Azurite.CreateContainerAsync("test-container");
        var blobContent = "0123456789";
        var blobName = await fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", blobContent);
        await fixture.SynchronizeCacheAsync();
        using HttpClient client = fixture.CreateClient();

        // Act - Request range beyond the blob size
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/containers/{containerName}/blobs/{blobName}/content");
        request.Headers.Add(HeaderNames.Range, "bytes=100-200");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>(ServiceFixture.JsonOptions);
        problemDetails.Should().NotBeNull();

        var root = problemDetails!.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status416RangeNotSatisfiable);
        root.GetProperty("title").GetString().Should().Be("Range Not Satisfiable");
    }

    #endregion
}
