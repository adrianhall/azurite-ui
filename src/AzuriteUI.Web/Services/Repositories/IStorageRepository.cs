using AzuriteUI.Web.Services.Repositories.Models;

namespace AzuriteUI.Web.Services.Repositories;

/// <summary>
/// The definition of the storage repository.
/// </summary>
public interface IStorageRepository
{
    #region Blob Access
    /// <summary>
    /// The queryable collection of blobs.
    /// </summary>
    IQueryable<BlobDTO> Blobs { get; }

    /// <summary>
    /// Deletes a blob from Azurite and removes it from the cache.
    /// </summary>
    /// <param name="containerName">The name of the container to delete.</param>
    /// <param name="blobName">The name of the blob to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the blob is deleted.</returns>
    Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the <see cref="BlobDTO"/> for the specified container and blob name.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve.</param>
    /// <param name="blobName">THe name of the blob within the container to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The blob DTO, or null if not found.</returns>
    Task<BlobDTO?> GetBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing blob in Azurite and updates the cache.
    /// </summary>
    /// <param name="dto">The blob properties to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated blob DTO.</returns>
    Task<BlobDTO> UpdateBlobAsync(UpdateBlobDTO dto, CancellationToken cancellationToken = default);
    #endregion

    #region Container Access
    /// <summary>
    /// The queryable collection of storage containers.
    /// </summary>
    IQueryable<ContainerDTO> Containers { get; }

    /// <summary>
    /// Creates a new container in Azurite and updates the cache.
    /// </summary>
    /// <param name="dto">The container properties to set.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created container DTO.</returns>
    Task<ContainerDTO> CreateContainerAsync(CreateContainerDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a container from Azurite and removes it from the cache.
    /// </summary>
    /// <param name="containerName">The name of the container to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the container is deleted.</returns>
    Task DeleteContainerAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the <see cref="ContainerDTO"/> for the specified container name.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The container DTO, or null if not found.</returns>
    Task<ContainerDTO?> GetContainerAsync(string containerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing container in Azurite and updates the cache.
    /// </summary>
    /// <param name="updateDto">The container properties to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated container DTO.</returns>
    Task<ContainerDTO> UpdateContainerAsync(UpdateContainerDTO updateDto, CancellationToken cancellationToken = default);
    #endregion

    #region Upload and Download
    /// <summary>
    /// The queryable collection of upload sessions.
    /// </summary>
    IQueryable<UploadDTO> Uploads { get; }

    /// <summary>
    /// Initiates a download of the specified blob from Azurite.
    /// </summary>
    /// <remarks>
    /// This method streams the blob data directly from Azurite to avoid loading large blobs into memory.  Since we
    /// do HTTP range requests, the BlobDownloadDTO can return a status code indicating partial content or full
    /// content or content range cannot be satisfied or not found.
    /// </remarks>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="blobName">The name of the blob to download.</param>
    /// <param name="httpRange">The HTTP range to download.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that represents the asynchronous download operation. The result contains the downloaded blob DTO, or null if not found.</returns>
    Task<BlobDownloadDTO> DownloadBlobAsync(string containerName, string blobName, string? httpRange = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an upload session and deletes all associated blocks.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the upload is cancelled.</returns>
    Task CancelUploadAsync(Guid uploadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits an upload session by assembling the blocks into a blob.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="blockIds">The ordered list of blocks to commit.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created blob DTO.</returns>
    Task<BlobDTO> CommitUploadAsync(Guid uploadId, IEnumerable<string> blockIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a chunked blob upload session.
    /// </summary>
    /// <param name="uploadDto">The upload request details.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created upload session details.</returns>
    Task<UploadStatusDTO> CreateUploadAsync(CreateUploadRequestDTO uploadDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the status of an upload session.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The upload status, or null if not found.</returns>
    Task<UploadStatusDTO> GetUploadStatusAsync(Guid uploadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a block (chunk) to an in-progress upload session.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="blockId">The Base64-encoded block identifier.</param>
    /// <param name="content">The block content stream.</param>
    /// <param name="contentMD5">Optional MD5 hash for integrity verification.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the block is uploaded.</returns>
    Task UploadBlockAsync(Guid uploadId, string blockId, Stream content, string? contentMD5 = null, CancellationToken cancellationToken = default);
    #endregion
}