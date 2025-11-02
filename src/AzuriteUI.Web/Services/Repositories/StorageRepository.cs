using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace AzuriteUI.Web.Services.Repositories;

/// <summary>
/// Concrete implementation of the storage repository.
/// </summary>
/// <remarks>
/// The primary purpose of this repository is to ensure that the cache database
/// is properly updated when changes are made to the Azurite storage.  This is
/// done in a write-through manner, where changes are first made to Azurite, and
/// then the cache database is updated to reflect those changes.
/// </remarks>
/// <param name="context">The cache database context.</param>
/// <param name="azurite">The Azurite service.</param>
/// <param name="logger">The logger.</param>
public class StorageRepository(
    CacheDbContext context,
    IAzuriteService azurite,
    ILogger<StorageRepository> logger
) : IStorageRepository
{
    #region Blob Access
    /// <summary>
    /// A queryable collection of blob DTOs.
    /// </summary>
    public IQueryable<BlobDTO> Blobs
    {
        get => context.Blobs.Select(blob => new BlobDTO
        {
            // IBaseDTO properties
            Name = blob.Name,
            ETag = blob.ETag,
            LastModified = blob.LastModified,

            // BlobDTO properties - mapped from stored values
            BlobType = blob.BlobType.ToString().ToLowerInvariant(),
            ContainerName = blob.ContainerName,
            ContentEncoding = blob.ContentEncoding,
            ContentLanguage = blob.ContentLanguage,
            ContentLength = blob.ContentLength,
            ContentType = blob.ContentType,
            CreatedOn = blob.CreatedOn,
            ExpiresOn = blob.ExpiresOn,
            HasLegalHold = blob.HasLegalHold,
            LastAccessedOn = blob.LastAccessedOn,
            Metadata = blob.Metadata,
            Tags = blob.Tags,
            RemainingRetentionDays = blob.RemainingRetentionDays
        });
    }

    /// <summary>
    /// Deletes a blob from Azurite and removes it from the cache.
    /// </summary>
    /// <param name="containerName">The name of the container to delete.</param>
    /// <param name="blobName">The name of the blob to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the blob is deleted.</returns>
    public async Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("DeleteBlobAsync('{containerName}', '{blobName}') called", containerName, blobName);
        await azurite.DeleteBlobAsync(containerName, blobName, cancellationToken);
        await context.RemoveBlobAsync(containerName, blobName, cancellationToken);
    }

    /// <summary>
    /// Retrieves the <see cref="BlobDTO"/> for the specified container and blob name.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve.</param>
    /// <param name="blobName">THe name of the blob within the container to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The blob DTO, or null if not found.</returns>
    public async Task<BlobDTO?> GetBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetBlobAsync('{containerName}', '{blobName}') called", containerName, blobName);
        return await Blobs.FirstOrDefaultAsync(b => b.ContainerName == containerName && b.Name == blobName, cancellationToken);
    }

    /// <summary>
    /// Updates an existing blob in Azurite and updates the cache.
    /// </summary>
    /// <param name="containerName">The name of the container to update.</param>
    /// <param name="blobName">The name of the blob to update.</param>
    /// <param name="updateDto">The blob properties to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated blob DTO.</returns>
    public async Task<BlobDTO> UpdateBlobAsync(string containerName, string blobName, BlobUpdateDTO updateDto, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UpdateBlobAsync('{containerName}', '{blobName}') called", containerName, blobName);
        var properties = new AzuriteBlobProperties
        {
            ContentEncoding = updateDto.ContentEncoding,
            ContentLanguage = updateDto.ContentLanguage,
            Metadata = updateDto.Metadata,
            Tags = updateDto.Tags,
        };
        var updatedBlob = await azurite.UpdateBlobAsync(containerName, blobName, properties, cancellationToken);
        await context.UpsertBlobAsync(updatedBlob, containerName, cancellationToken);
        // Single is ok here because we've done an Upsert on the database.
        return await Blobs.SingleAsync(b => b.ContainerName == containerName && b.Name == blobName, cancellationToken);
    }
    #endregion

    #region Container Access
    /// <summary>
    /// A queryable collection of container DTOs.
    /// </summary>
    public IQueryable<ContainerDTO> Containers
    {
        get => context.Containers.Select(container => new ContainerDTO
        {
            // IBaseDTO properties
            Name = container.Name,
            ETag = container.ETag,
            LastModified = container.LastModified,

            // ContainerDTO properties - mapped from stored values
            BlobCount = container.BlobCount,
            TotalSize = container.TotalSize,
            DefaultEncryptionScope = container.DefaultEncryptionScope,
            HasImmutabilityPolicy = container.HasImmutabilityPolicy,
            HasImmutableStorageWithVersioning = container.HasImmutableStorageWithVersioning,
            HasLegalHold = container.HasLegalHold,
            Metadata = container.Metadata,
            PreventEncryptionScopeOverride = container.PreventEncryptionScopeOverride,
            PublicAccess = container.PublicAccess.ToString().ToLowerInvariant(),
            RemainingRetentionDays = container.RemainingRetentionDays
        });
    }

    
    /// <summary>
    /// Creates a new container in Azurite and updates the cache.
    /// </summary>
    /// <param name="containerName">The name of the container to create.</param>
    /// <param name="updateDto">The container properties to set.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created container DTO.</returns>
    public async Task<ContainerDTO> CreateContainerAsync(string containerName, ContainerUpdateDTO updateDto, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("CreateContainerAsync('{containerName}') called", containerName);
        var containerProperties = new AzuriteContainerProperties
        {
            PublicAccessType = updateDto.PublicAccess is not null ? ConvertToPublicAccessType(updateDto.PublicAccess) : null,
            DefaultEncryptionScope = updateDto.DefaultEncryptionScope,
            PreventEncryptionScopeOverride = updateDto.PreventEncryptionScopeOverride,
            Metadata = updateDto.Metadata
        };
        var azuriteContainer = await azurite.CreateContainerAsync(containerName, containerProperties, cancellationToken);
        await context.UpsertContainerAsync(azuriteContainer, cancellationToken);
        return await Containers.SingleAsync(c => c.Name == containerName, cancellationToken);
    }

    /// <summary>
    /// Deletes a container from Azurite and removes it from the cache.
    /// </summary>
    /// <param name="containerName">The name of the container to delete.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the container is deleted.</returns>
    public async Task DeleteContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("DeleteContainerAsync('{containerName}') called", containerName);
        await azurite.DeleteContainerAsync(containerName, cancellationToken);
        await context.RemoveContainerAsync(containerName, cancellationToken);
    }

    /// <summary>
    /// Retrieves the <see cref="ContainerDTO"/> for the specified container name.
    /// </summary>
    /// <param name="containerName">The name of the container to retrieve.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The container DTO, or null if not found.</returns>
    public async Task<ContainerDTO?> GetContainerAsync(string containerName, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetContainerAsync('{containerName}') called", containerName);
        return await Containers.FirstOrDefaultAsync(c => c.Name == containerName, cancellationToken);
    }

    /// <summary>
    /// Updates an existing container in Azurite and updates the cache.
    /// </summary>
    /// <param name="containerName">The name of the container to update.</param>
    /// <param name="updateDto">The container properties to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated container DTO.</returns>
    public async Task<ContainerDTO> UpdateContainerAsync(string containerName, ContainerUpdateDTO updateDto, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UpdateContainerAsync('{containerName}') called", containerName);
        var properties = new AzuriteContainerProperties
        {
            Metadata = updateDto.Metadata
        };
        var azuriteContainer = await azurite.UpdateContainerAsync(containerName, properties, cancellationToken);
        await context.UpsertContainerAsync(azuriteContainer, cancellationToken);
        // Single is ok here because we've done an Upsert on the database.
        return await Containers.SingleAsync(c => c.Name == containerName, cancellationToken);
    }
    #endregion

    /// <summary>
    /// Converts the given public access string to the corresponding <see cref="AzuritePublicAccess"/> enum value.
    /// </summary>
    /// <param name="publicAccess">The inbound string.</param>
    /// <returns>The <see cref="AzuritePublicAccess"/> enum value.</returns>
    /// <exception cref="AzuriteServiceException">Thrown if the public access type is invalid.</exception>
    internal static AzuritePublicAccess ConvertToPublicAccessType(string publicAccess)
    {
        return publicAccess.ToLowerInvariant() switch
        {
            "container" => AzuritePublicAccess.Container,
            "blobcontainer" => AzuritePublicAccess.Container,
            "blob" => AzuritePublicAccess.Blob,
            "none" => AzuritePublicAccess.None,
            _ => throw new AzuriteServiceException($"Invalid public access type: {publicAccess}") { StatusCode = StatusCodes.Status400BadRequest }
        };
    }
}