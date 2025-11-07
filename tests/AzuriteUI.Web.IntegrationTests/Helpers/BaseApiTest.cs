using System.Net.Http.Json;
using AzuriteUI.Web.Services.Repositories.Models;

namespace AzuriteUI.Web.IntegrationTests.Helpers;

/// <summary>
/// Base class for API integration tests that provides common helper methods.
/// </summary>
/// <param name="serviceFixture">The service fixture.</param>
[ExcludeFromCodeCoverage(Justification = "Test base class")]
public abstract class BaseApiTest(ServiceFixture serviceFixture) : IClassFixture<ServiceFixture>, IAsyncLifetime
{
    /// <summary>
    /// The service fixture to use.
    /// </summary>
    public ServiceFixture Fixture { get => serviceFixture; }

    #region IAsyncLifetime Implementation
    /// <inheritdoc/>
    /// <remarks> 
    /// This method is called before each test is run.  It cleans up the Azurite storage
    /// and cache database to ensure a fresh state for each test.
    /// </remarks>
    public async ValueTask InitializeAsync()
    {
        await serviceFixture.CleanupAsync();
    }

    /// <summary>
    /// Part of <see cref="IAsyncLifetime"/>
    /// </summary>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
    #endregion

    #region Common Helper Methods
    /// <summary>
    /// Creates multiple containers in Azurite and optionally synchronizes the cache.
    /// </summary>
    /// <param name="containerNames">The container names to create.</param>
    /// <param name="synchronize">If true, synchronize the cache.</param>
    protected async Task CreateContainersAsync(IEnumerable<string> containerNames, bool synchronize = true)
    {
        foreach (var containerName in containerNames)
        {
            await Fixture.Azurite.CreateContainerAsync(containerName);
        }

        if (synchronize)
        {
            await Fixture.SynchronizeCacheAsync();
        }
    }

    /// <summary>
    /// Ensures an ETag is properly quoted for HTTP headers.
    /// </summary>
    /// <param name="etag">The ETag value to quote.</param>
    /// <returns>The quoted ETag, or the original value if null/empty or already quoted.</returns>
    protected static string EnsureQuotedETag(string etag)
    {
        if (string.IsNullOrEmpty(etag))
            return etag;

        // If it's already quoted, return as-is
        if (etag.StartsWith("\"") && etag.EndsWith("\""))
            return etag;

        // Otherwise, add quotes
        return $"\"{etag}\"";
    }

    /// <summary>
    /// Creates an upload session for testing block upload scenarios.
    /// </summary>
    /// <param name="client">The HTTP client to use.</param>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobName">The blob name.</param>
    /// <param name="contentLength">The expected total content length.</param>
    /// <returns>The upload session ID.</returns>
    protected static async Task<Guid> CreateUploadSessionAsync(HttpClient client, string containerName, string blobName, long contentLength)
    {
        var createDto = new CreateUploadRequestDTO
        {
            BlobName = blobName,
            ContainerName = containerName,
            ContentLength = contentLength,
            ContentType = "text/plain"
        };
        var createResponse = await client.PostAsJsonAsync($"/api/containers/{containerName}/blobs", createDto);
        var createdUpload = await createResponse.Content.ReadFromJsonAsync<UploadStatusDTO>(ServiceFixture.JsonOptions);
        return createdUpload!.UploadId;
    }

    /// <summary>
    /// Uploads a block of random data to an upload session.
    /// </summary>
    /// <param name="client">The HTTP client to use.</param>
    /// <param name="uploadId">The upload session ID.</param>
    /// <param name="blockId">The block ID (should be Base64 encoded).</param>
    /// <param name="size">The size of the block in bytes.</param>
    protected static async Task UploadBlockAsync(HttpClient client, Guid uploadId, string blockId, int size)
    {
        var blockData = new byte[size];
        new Random().NextBytes(blockData);
        var content = new ByteArrayContent(blockData);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        await client.PutAsync($"/api/uploads/{uploadId}/blocks/{blockId}", content);
    }

    #endregion
}
