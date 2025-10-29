using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Azurite.Models;

namespace AzuriteUI.Web.Services.Azurite;

/// <summary>
/// An interface for Azurite service operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface is a mockable contract for Azurite service operations and
/// is the only service that interacts directly with Azurite.  It combines the
/// requirements for BlobServiceClient, BlobContainerClient, BlobClient, and
/// BlockBlobClient into a single interface to simplify mocking and testing.
/// </para>
/// </remarks>
public interface IAzuriteService
{
    #region Azurite Properties and Health
    /// <summary>
    /// The connection string used to connect to the Azurite service.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Retrieves the health status of the Azurite service.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The health status of the Azurite service.</returns>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the health status.</exception>
    Task<AzuriteHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
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
    Task<AzuriteContainerItem> CreateContainerAsync(string containerName, AzuriteContainerProperties properties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the Azurite container with the specified name.
    /// </summary>
    /// <param name="containerName">The name of the container to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if a container with the specified name does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error deleting the container.</exception>
    Task DeleteContainerAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the Azurite container with the specified name.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The requested Azurite container item.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if a container with the specified name does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the container.</exception>
    Task<AzuriteContainerItem> GetContainerAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an asynchronous enumerable of Azurite container items.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An asynchronous enumerable of Azurite container items.</returns>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the containers.</exception>
    IAsyncEnumerable<AzuriteContainerItem> GetContainersAsync(CancellationToken cancellationToken = default);

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
    Task<AzuriteContainerItem> UpdateContainerAsync(string containerName, AzuriteContainerProperties properties, CancellationToken cancellationToken = default);
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
    Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

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
    Task<AzuriteBlobDownloadResult> DownloadBlobAsync(string containerName, string blobName, string? httpRange = null, CancellationToken cancellationToken = default);

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
    Task<AzuriteBlobItem> GetBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an asynchronous enumerable of blobs in the specified container.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve blobs from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>An asynchronous enumerable of blobs in the specified container.</returns>
    /// <exception cref="ArgumentException">Thrown if the container name is invalid.</exception>
    /// <exception cref="ResourceNotFoundException">Thrown if the specified container does not exist.</exception>
    /// <exception cref="AzuriteServiceException">Thrown if there is an error retrieving the blobs.</exception>
    IAsyncEnumerable<AzuriteBlobItem> GetBlobsAsync(string containerName, CancellationToken cancellationToken = default);

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
    Task<AzuriteBlobItem> UpdateBlobAsync(string containerName, string blobName, AzuriteBlobProperties properties, CancellationToken cancellationToken = default);

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
    Task<AzuriteBlobBlockInfo> UploadBlockAsync(string containerName, string blobName, string blockId, Stream content, CancellationToken cancellationToken = default);

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
    Task UploadCheckAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

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
    Task<AzuriteBlobItem> UploadCommitAsync(string containerName, string blobName, IEnumerable<string> blockIds, AzuriteBlobProperties properties, CancellationToken cancellationToken = default);
    #endregion
}
