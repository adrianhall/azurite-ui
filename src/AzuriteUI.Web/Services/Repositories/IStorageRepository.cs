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
    /// <param name="containerName">The name of the container to update.</param>
    /// <param name="blobName">The name of the blob to update.</param>
    /// <param name="updateDto">The blob properties to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated blob DTO.</returns>
    Task<BlobDTO> UpdateBlobAsync(string containerName, string blobName, BlobUpdateDTO updateDto, CancellationToken cancellationToken = default);
    #endregion

    #region Container Access
    /// <summary>
    /// The queryable collection of storage containers.
    /// </summary>
    IQueryable<ContainerDTO> Containers { get; }

    /// <summary>
    /// Creates a new container in Azurite and updates the cache.
    /// </summary>
    /// <param name="containerName">The name of the container to create.</param>
    /// <param name="updateDto">The container properties to set.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created container DTO.</returns>
    Task<ContainerDTO> CreateContainerAsync(string containerName, ContainerUpdateDTO updateDto, CancellationToken cancellationToken = default);

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
    /// <param name="containerName">The name of the container to update.</param>
    /// <param name="updateDto">The container properties to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated container DTO.</returns>
    Task<ContainerDTO> UpdateContainerAsync(string containerName, ContainerUpdateDTO updateDto, CancellationToken cancellationToken = default);
    #endregion

}