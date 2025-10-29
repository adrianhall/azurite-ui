using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Azurite.Models;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AzuriteUI.Web.Services.Azurite;

/// <summary>
/// The concrete implementation of the <see cref="IAzuriteService"/> that communicates with
/// a real Azurite service.
/// </summary>
public class AzuriteService : IAzuriteService
{
    /// <summary>
    /// The name of the Azurite ConnectionString name.
    /// </summary>
    /// <example>
    /// ```json
    /// {
    ///   "ConnectionStrings": {
    ///     "Azurite": "UseDevelopmentStorage=true;"
    ///   }
    /// }
    /// ```
    /// </example>
    private const string AzuriteConnectionStringName = "Azurite";

    /// <summary>
    /// Creates a new instance of <see cref="AzuriteService"/> using the connection string
    /// for the Azurite service.
    /// </summary>
    /// <param name="connectionString">The Azurite connection string to use.</param>
    /// <param name="logger">The logger to use for diagnostics and reporting.</param>
    public AzuriteService(string connectionString, ILogger<AzuriteService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        ConnectionString = ValidateConnectionString(connectionString);
        Logger = logger;
        ServiceClient = new BlobServiceClient(ConnectionString);
    }

    /// <summary>
    /// Creates a new instance of <see cref="AzuriteService"/> using the ASP.NET Core
    /// <see cref="IConfiguration"/> as a holder for the connection string.
    /// </summary>
    /// <param name="configuration">The configuration holding the connection string.</param>
    /// <param name="logger">The logger to use for diagnostics and reporting.</param>
    public AzuriteService(IConfiguration configuration, ILogger<AzuriteService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        string connectionString = configuration.GetRequiredConnectionString(AzuriteConnectionStringName);
        ConnectionString = ValidateConnectionString(connectionString);
        Logger = logger;
        ServiceClient = new BlobServiceClient(ConnectionString);
    }

    /// <summary>
    /// The logger to use for diagnostics and reporting.
    /// </summary>
    internal ILogger Logger { get; }

    /// <summary>
    /// The BlobServiceClient used to communicate with the Azurite service.
    /// </summary>
    internal BlobServiceClient ServiceClient { get; }

    #region Azurite Properties and Health
    /// <summary>
    /// The connection string used to connect to the Azurite service.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Retrieves the health status of the Azurite service.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The health status of the Azurite service.</returns>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the health status.</exception>
    public async Task<AzuriteHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("GetHealthStatusAsync()");
        var health = new AzuriteHealthStatus { ConnectionString = ConnectionString };
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await CheckServiceIsAliveAsync(cancellationToken).ConfigureAwait(false);
            health.IsHealthy = true;
            stopwatch.Stop();
            health.ResponseTimeMilliseconds = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            health.IsHealthy = false;
            health.ErrorMessage = ex.Message;
            Logger.LogError(ex, "Azurite service is not healthy");
        }
        return health;
    }
    #endregion

    #region Container Management
    /// <summary>
    /// Creates a new Azurite container with the specified name.
    /// </summary>
    /// <param name="containerName">The name of the container to create.</param>
    /// <param name="properties">The properties for the container to create.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created Azurite container item.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name is invalid.</exception>
    /// <exception cref="ResourceExistsException">Thrown if a container with the specified name already exists.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error creating the container.</exception>
    public async Task<AzuriteContainerItem> CreateContainerAsync(string containerName, AzuriteContainerProperties properties, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("CreateContainerAsync({ContainerName}, {Properties})", containerName, JsonSerializer.Serialize(properties));
        return await HandleRequestFailedExceptionAsync(containerName, async () =>
        {
            var publicAccess = ConvertToPublicAccessType(properties.PublicAccessType);
            var encryptionScope = ConvertToEncryptionScope(properties.DefaultEncryptionScope, properties.PreventEncryptionScopeOverride);
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            _ = await containerClient.CreateAsync(publicAccess, properties.Metadata, encryptionScope, cancellationToken).ConfigureAwait(false);
            return await GetContainerAsync(containerName, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the Azurite container with the specified name.
    /// </summary>
    /// <param name="containerName">The name of the container to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if a container with the specified name does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error deleting the container.</exception>
    public async Task DeleteContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("DeleteContainerAsync({ContainerName})", containerName);
        await HandleRequestFailedExceptionAsync(containerName, async () =>
        {
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            _ = await containerClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the Azurite container with the specified name.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The requested Azurite container item.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if a container with the specified name does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the container.</exception>
    public async Task<AzuriteContainerItem> GetContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("GetContainerAsync({ContainerName})", containerName);
        return await HandleRequestFailedExceptionAsync(containerName, async () =>
        {
            var containerItem = await ServiceClient
                .GetBlobContainersAsync(prefix: containerName, traits: BlobContainerTraits.Metadata, states: BlobContainerStates.None, cancellationToken: cancellationToken)
                .FirstOrDefaultAsync(c => c.Name == containerName, cancellationToken);
            return containerItem is null
                ? throw new ResourceNotFoundException("The specified container was not found.") { ResourceName = containerName }
                : AzuriteContainerItem.FromAzure(containerItem);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves an asynchronous enumerable of Azurite container items.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An asynchronous enumerable of Azurite container items.</returns>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the containers.</exception>
    public async IAsyncEnumerable<AzuriteContainerItem> GetContainersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("GetContainersAsync()");
        var containers = ServiceClient.GetBlobContainersAsync(traits: BlobContainerTraits.Metadata, states: BlobContainerStates.None, cancellationToken: cancellationToken);
        await foreach (var containerItem in containers.WithCancellation(cancellationToken))
        {
            yield return AzuriteContainerItem.FromAzure(containerItem);
        }
    }

    /// <summary>
    /// Updates the properties of the specified Azurite container.
    /// </summary>
    /// <param name="containerName">The name of the container to update.</param>
    /// <param name="properties">The new properties for the container.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated Azurite container item.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if a container with the specified name does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error updating the container.</exception>
    /// <remarks>
    /// Not all properties can be updated through this method.  You will receive an <see cref="AzuriteServiceException"/>
    /// if you attempt to update a property that is not supported for update.
    /// </remarks>
    public async Task<AzuriteContainerItem> UpdateContainerAsync(string containerName, AzuriteContainerProperties properties, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("UpdateContainerAsync({ContainerName}, {Properties})", containerName, JsonSerializer.Serialize(properties));
        return await HandleRequestFailedExceptionAsync(containerName, async () =>
        {
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            _ = await containerClient.SetMetadataAsync(properties.Metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await GetContainerAsync(containerName, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region Blob Management
    /// <summary>
    /// Deletes the specified blob from the given container.
    /// </summary>
    /// <param name="containerName">The name of the container to update.</param>
    /// <param name="blobName">The name of the blob to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when work is complete.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name or blob name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the specified blob or container does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error deleting the blob.</exception>
    public async Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("DeleteBlobAsync({ContainerName}, {BlobName})", containerName, blobName);
        await HandleRequestFailedExceptionAsync("{ContainerName}/{BlobName}", async () =>
        {
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            _ = await containerClient.DeleteBlobIfExistsAsync(blobName, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads (a range of) the specified blob from the given container.
    /// </summary>
    /// <param name="containerName">The name of the container to download from.</param>
    /// <param name="blobName">The name of the blob to download.</param>
    /// <param name="httpRange">The range of bytes to download.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name or blob name is invalid, or the provided HTTP Range is not valid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the specified blob or container does not exist.</exception>
    /// <exception cref="RangeNotSatisfiableException">Thrown if the specified range is invalid.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error downloading the blob.</exception>
    public async Task<AzuriteBlobDownloadResult> DownloadBlobAsync(string containerName, string blobName, string? httpRange = null, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("DownloadBlobAsync({ContainerName}, {BlobName}, {HttpRange})", containerName, blobName, httpRange ?? "<full>");
        return await HandleRequestFailedExceptionAsync($"{containerName}/{blobName}", async () =>
        {
            var downloadOptions = string.IsNullOrWhiteSpace(httpRange)
                ? new BlobDownloadOptions()
                : new BlobDownloadOptions { Range = ParseHttpRange(httpRange) };

            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var result = await blobClient.DownloadStreamingAsync(downloadOptions, cancellationToken).ConfigureAwait(false);
            return new AzuriteBlobDownloadResult
            {
                Content = result.Value.Content,
                ContentLength = result.Value.Details.ContentLength,
                ContentRange = result.Value.Details.ContentRange,
                ContentType = result.Value.Details.ContentType,
                StatusCode = result.GetRawResponse().Status
            };
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the specified blob properties from the given container.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve the blob from.</param>
    /// <param name="blobName">The name of the blob to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that represents the asynchronous operation, with a value of the blob properties.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name or blob name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the specified blob or container does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the blob properties.</exception>
    public async Task<AzuriteBlobItem> GetBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("GetBlobAsync({ContainerName}, {BlobName})", containerName, blobName);
        return await HandleRequestFailedExceptionAsync($"{containerName}/{blobName}", async () =>
        {
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            var blobItem = await containerClient
                .GetBlobsAsync(traits: BlobTraits.All, states: BlobStates.None, prefix: blobName, cancellationToken: cancellationToken)
                .FirstOrDefaultAsync(b => b.Name == blobName, cancellationToken).ConfigureAwait(false);
            return blobItem is null
                ? throw new ResourceNotFoundException("The specified blob was not found.") { ResourceName = $"{containerName}/{blobName}" }
                : AzuriteBlobItem.FromAzure(blobItem);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves an asynchronous enumerable of blobs in the specified container.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve blobs from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An asynchronous enumerable of blobs in the specified container.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the specified container does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the blobs.</exception>
    public async IAsyncEnumerable<AzuriteBlobItem> GetBlobsAsync(string containerName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("GetBlobsAsync({ContainerName})", containerName);
        var containerClient = ServiceClient.GetBlobContainerClient(containerName);
        bool exists = await containerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            throw new ResourceNotFoundException("The specified container was not found.") { ResourceName = containerName };
        }
        
        var blobs = containerClient.GetBlobsAsync(traits: BlobTraits.All, states: BlobStates.None, cancellationToken: cancellationToken);
        await foreach (var blobItem in blobs.WithCancellation(cancellationToken))
        {
            yield return AzuriteBlobItem.FromAzure(blobItem);
        }
    }

    /// <summary>
    /// Updates the properties of the specified blob in the given container.
    /// </summary>
    /// <param name="containerName">The name of the container that contains the blob.</param>
    /// <param name="blobName">The name of the blob to update.</param>
    /// <param name="properties">The new properties for the blob.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that represents the asynchronous operation, with a value of the updated blob.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name or blob name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the specified blob or container does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error updating the blob.</exception>
    /// <remarks>
    /// Not all properties can be updated through this method.  You will receive an <see cref="AzuriteServiceException"/>
    /// if you attempt to update a property that is not supported for update.
    /// </remarks>
    public async Task<AzuriteBlobItem> UpdateBlobAsync(string containerName, string blobName, AzuriteBlobProperties properties, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("UpdateBlobAsync({ContainerName}, {BlobName}, {Properties})", containerName, blobName, JsonSerializer.Serialize(properties));
        return await HandleRequestFailedExceptionAsync($"{containerName}/{blobName}", async () =>
        {
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            _ = await blobClient.SetMetadataAsync(properties.Metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
            _ = await blobClient.SetTagsAsync(properties.Tags, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await GetBlobAsync(containerName, blobName, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Uploads a single chunk (block) of blob data to Azurite. This method stages the block but does not
    /// finalize the blob. Multiple blocks can be staged before calling <see cref="UploadCommitAsync"/> to
    /// commit them all at once. Each block ID must be Base64-encoded and unique within the blob.
    /// </summary>
    /// <param name="containerName">The name of the container where the blob is being uploaded.</param>
    /// <param name="blobName">The name of the blob being uploaded.</param>
    /// <param name="blockId">The Base64-encoded block ID for this chunk. Must be unique within the blob.</param>
    /// <param name="content">The stream containing the chunk data to upload.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves to a <see cref="AzuriteBlobBlockInfo"/> with upload details.</returns>
    public async Task<AzuriteBlobBlockInfo> UploadBlockAsync(string containerName, string blobName, string blockId, Stream content, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("UploadBlockAsync(containerName: {ContainerName}, blobName: {BlobName}, blockId: {BlockId})", containerName, blobName, blockId);
        return await HandleRequestFailedExceptionAsync($"{containerName}/{blobName}/blocks/{blockId}", async () =>
        {
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlockBlobClient(blobName);
            var response = await blobClient.StageBlockAsync(blockId, content, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new AzuriteBlobBlockInfo
            {
                BlockId = blockId,
                ContentMD5 = response.Value.ContentHash.AsOptionalBase64(),
                StatusCode = response.GetRawResponse().Status
            };
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if a blob exists in the specified container.  This is normally used as part of the upload
    /// process to initiate a new upload session.
    /// </summary>
    /// <param name="containerName">The name of the container to upload the blob to.</param>
    /// <param name="blobName">The name of the blob to upload.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ResourceExistsException">Thrown if the blob already exists..</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the specified container does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error initiating the upload.</exception>
    public async Task UploadCheckAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("InitiateUploadAsync({ContainerName}, {BlobName})", containerName, blobName);
        await HandleRequestFailedExceptionAsync($"{containerName}/{blobName}", async () =>
        {
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            bool exists = await containerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new ResourceNotFoundException("The specified container does not exist.") { ResourceName = containerName };
            }

            var blobClient = containerClient.GetBlobClient(blobName);
            exists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                throw new ResourceExistsException("A blob with the specified name already exists.") { ResourceName = $"{containerName}/{blobName}" };
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Commits the uploaded blocks to finalize the blob upload.
    /// </summary>
    /// <param name="containerName">The name of the container that contains the blob.</param>
    /// <param name="blobName">The name of the blob to commit the upload for.</param>
    /// <param name="blockIds">The IDs of the blocks to commit.</param>
    /// <param name="properties">The properties to set on the blob.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that represents the asynchronous operation, with a value of the committed blob.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name or blob name is invalid.</exception>
    /// <exception cref="ResourceExistsException">Thrown if a blob with the specified name already exists.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error committing the upload.</exception>
    public async Task<AzuriteBlobItem> UploadCommitAsync(string containerName, string blobName, IEnumerable<string> blockIds, AzuriteBlobProperties properties, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("UploadCommitAsync({ContainerName}, {BlobName}, blockIds: [{BlockIds}], {Properties})", containerName, blobName, string.Join(", ", blockIds), JsonSerializer.Serialize(properties));
        return await HandleRequestFailedExceptionAsync($"{containerName}/{blobName}", async () =>
        {
            var containerClient = ServiceClient.GetBlobContainerClient(containerName);
            var blockBlobClient = containerClient.GetBlockBlobClient(blobName);

            var commitOptions = new CommitBlockListOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = properties.ContentType ?? "application/octet-stream",
                    ContentEncoding = properties.ContentEncoding ?? string.Empty,
                    ContentLanguage = properties.ContentLanguage ?? "en-US"
                },
                Metadata = properties.Metadata,
                Tags = properties.Tags
            };

            var response = await blockBlobClient.CommitBlockListAsync(blockIds, commitOptions, cancellationToken);
            return await GetBlobAsync(containerName, blobName, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        throw new NotImplementedException();
    }
    #endregion

    /// <summary>
    /// Checks if the Azurite service is alive by attempting to retrieve account information.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that resolves when the Azurite service is alive.</returns>
    protected virtual async Task CheckServiceIsAliveAsync(CancellationToken cancellationToken = default)
    {
        _ = await ServiceClient.GetAccountInfoAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a <see cref="RequestFailedException"/> from the Azure SDK into
    /// a more specific Azurite exception.
    /// </summary>
    /// <param name="ex">The <see cref="RequestFailedException"/> that was thrown.</param>
    /// <param name="resourceName">The name of the resource that was requested.</param>
    /// <returns>An alternative exception that is more specific.</returns>
    internal static Exception ConvertAzuriteException(RequestFailedException ex, string? resourceName = null)
    {
        return ex.Status switch
        {
            404 => new ResourceNotFoundException("The specified resource was not found.", ex) { ResourceName = resourceName },
            409 => new ResourceExistsException("The specified resource already exists.", ex) { ResourceName = resourceName },
            416 => new RangeNotSatisfiableException("The specified range is not satisfiable.", ex),
            _ => new AzuriteServiceException("An error occurred while communicating with the Azurite service.", ex) { StatusCode = ex.Status },
        };
    }
    /// <summary>
    /// Converts the provided default encryption scope and prevent override flag
    /// into a <see cref="BlobContainerEncryptionScopeOptions"/> instance.
    /// </summary>
    /// <param name="defaultEncryptionScope">The default encryption scope to use.</param>
    /// <param name="preventEncryptionScopeOverride">A flag indicating whether to prevent encryption scope override.</param>
    /// <returns>A <see cref="BlobContainerEncryptionScopeOptions"/> instance, or null if the default encryption scope is not set.</returns>
    internal static BlobContainerEncryptionScopeOptions? ConvertToEncryptionScope(string? defaultEncryptionScope, bool? preventEncryptionScopeOverride)
    {
        // Return null if the default encryption scope is not set.
        return string.IsNullOrEmpty(defaultEncryptionScope)
            ? null
            : new BlobContainerEncryptionScopeOptions
            {
                DefaultEncryptionScope = defaultEncryptionScope,
                PreventEncryptionScopeOverride = preventEncryptionScopeOverride ?? false,
            };
    }

    /// <summary>
    /// Converts the internal <see cref="AzuritePublicAccess"/> enum to the Azure SDK's <see cref="PublicAccessType"/> enum.
    /// </summary>
    /// <param name="publicAccess">The incoming enum value.</param>
    /// <returns>The converted enum value.</returns>
    internal static PublicAccessType ConvertToPublicAccessType(AzuritePublicAccess? publicAccess)
    {
        return publicAccess switch
        {
            AzuritePublicAccess.None => PublicAccessType.None,
            AzuritePublicAccess.Blob => PublicAccessType.Blob,
            AzuritePublicAccess.Container => PublicAccessType.BlobContainer,
            _ => PublicAccessType.None,
        };
    }

    /// <summary>
    /// Handles a <see cref="RequestFailedException"/> thrown by the Azure SDK
    /// and converts it into a more specific Azurite exception.
    /// </summary>
    /// <typeparam name="T">The type of response required by the method.</typeparam>
    /// <param name="resourceName">The name of the resource being requested.</param>
    /// <param name="func">A function to execute that may throw a RequestFailedException.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="RangeNotSatisfiableException">Thrown if the specified range is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the resource being accessed does not exist.</exception>
    /// <exception cref="ResourceExistsException">Thrown if the resource being created already exists.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error communicating with the Azurite service.</exception>
    internal static async Task<T> HandleRequestFailedExceptionAsync<T>(string? resourceName, Func<Task<T>> func, CancellationToken cancellationToken = default)
    {
        try
        {
            return await func().ConfigureAwait(false);
        }
        catch (RequestFailedException ex)
        {
            throw ConvertAzuriteException(ex, resourceName);
        }
    }

    /// <summary>
    /// Handles a <see cref="RequestFailedException"/> thrown by the Azure SDK
    /// and converts it into a more specific Azurite exception.
    /// </summary>
    /// <param name="resourceName">The name of the resource being requested.</param>
    /// <param name="func">A function to execute that may throw a RequestFailedException.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <exception cref="RangeNotSatisfiableException">Thrown if the specified range is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the resource being accessed does not exist.</exception>
    /// <exception cref="ResourceExistsException">Thrown if the resource being created already exists.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error communicating with the Azurite service.</exception>
    internal static async Task HandleRequestFailedExceptionAsync(string? resourceName, Func<Task> func, CancellationToken cancellationToken = default)
    {
        try
        {
            await func().ConfigureAwait(false);
        }
        catch (RequestFailedException ex)
        {
            throw ConvertAzuriteException(ex, resourceName);
        }
    }

    /// <summary>
    /// Parses the HTTP Range header value into an <see cref="Azure.HttpRange"/> object.
    /// </summary>
    /// <remarks>
    /// This method only supports a single range with an explicit start offset.
    /// <list type="bullet">
    /// <item>Closed ranges are supported (e.g., "bytes=0-499" for bytes 0-499)</item>
    /// <item>Open-ended ranges are supported (e.g., "bytes=500-" for bytes from 500 to end)</item>
    /// <item>Suffix ranges are NOT supported (e.g., "bytes=-500" for last 500 bytes)</item>
    /// <item>Multiple ranges are NOT supported (e.g., "bytes=0-499,500-999")</item>
    /// </list>
    /// If the range is open-ended, the length will be set to null, allowing the
    /// Azurite instance to determine the length based on the blob size.
    /// </remarks>
    /// <param name="httpRange">The incoming HTTP range.</param>
    /// <returns>The <see cref="Azure.HttpRange"/> object.</returns>
    /// <exception cref="ArgumentException">Thrown if the HTTP range is invalid or not supported.</exception>
    internal static HttpRange ParseHttpRange(string httpRange)
    {
        // Expected format: "bytes=start-end"
        if (string.IsNullOrWhiteSpace(httpRange) || !httpRange.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid Range header format: {httpRange}.", nameof(httpRange));
        }

        try
        {
            RangeHeaderValue rangeHeaderValue = RangeHeaderValue.Parse(httpRange);
            if (rangeHeaderValue.Ranges.Count != 1)
            {
                throw new ArgumentException("Only a single range is supported.", nameof(httpRange));
            }

            var range = rangeHeaderValue.Ranges.First();
            long offset = range.From 
                ?? throw new ArgumentException("Suffix ranges (e.g., 'bytes=-500') are not supported. Please specify an explicit start offset.", nameof(httpRange));
            long? length = range.To.HasValue ? (range.To.Value - offset + 1) : null;
            return new HttpRange(offset, length);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"Invalid Range header format: {httpRange}.", nameof(httpRange), ex);
        }
    }

    /// <summary>
    /// Validates that the connection string is a valid connection string.
    /// </summary>
    /// <param name="connectionString">The connection string that was provided.</param>
    /// <returns>The validated connection string.</returns>
    /// <exception cref="ArgumentException">Thrown if the connection string is invalid.</exception>
    internal static string ValidateConnectionString(string connectionString)
        => AzuriteConnectionStringBuilder.Parse(connectionString).ToString();
}
