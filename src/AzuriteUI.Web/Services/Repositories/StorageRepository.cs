using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheDb.Models;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        ValidateContainerName(containerName);
        ValidateBlobName(blobName);
        try
        {
            await azurite.DeleteBlobAsync(containerName, blobName, cancellationToken);
            await context.RemoveBlobAsync(containerName, blobName, cancellationToken);
        }
        catch (AzuriteServiceException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            // Ensure the blob is removed from the cache even if it was not found in Azurite.
            await context.RemoveBlobAsync(containerName, blobName, cancellationToken);
        }
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
        ValidateContainerName(containerName);
        ValidateBlobName(blobName);
        return await Blobs.FirstOrDefaultAsync(b => b.ContainerName == containerName && b.Name == blobName, cancellationToken);
    }

    /// <summary>
    /// Updates an existing blob in Azurite and updates the cache.
    /// </summary>
    /// <param name="dto">The blob properties to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated blob DTO.</returns>
    public async Task<BlobDTO> UpdateBlobAsync(UpdateBlobDTO dto, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UpdateBlobAsync('{containerName}', '{blobName}') called", dto.ContainerName, dto.BlobName);
        ValidateContainerName(dto.ContainerName);
        ValidateBlobName(dto.BlobName);
        var properties = new AzuriteBlobProperties
        {
            Metadata = dto.Metadata,
            Tags = dto.Tags,
        };
        var updatedBlob = await azurite.UpdateBlobAsync(dto.ContainerName, dto.BlobName, properties, cancellationToken);
        await context.UpsertBlobAsync(updatedBlob, dto.ContainerName, cancellationToken);
        // Single is ok here because we've done an Upsert on the database.
        return await Blobs.SingleAsync(b => b.ContainerName == dto.ContainerName && b.Name == dto.BlobName, cancellationToken);
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
    /// <param name="dto">The container properties to set.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created container DTO.</returns>
    public async Task<ContainerDTO> CreateContainerAsync(CreateContainerDTO dto, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("CreateContainerAsync({container}) called", JsonSerializer.Serialize(dto));
        ValidateContainerName(dto.ContainerName);

        var containerProperties = new AzuriteContainerProperties
        {
            PublicAccessType = dto.PublicAccess is not null ? ConvertToPublicAccessType(dto.PublicAccess) : null,
            DefaultEncryptionScope = dto.DefaultEncryptionScope,
            PreventEncryptionScopeOverride = dto.PreventEncryptionScopeOverride,
            Metadata = dto.Metadata
        };
        var azuriteContainer = await azurite.CreateContainerAsync(dto.ContainerName, containerProperties, cancellationToken);
        await context.UpsertContainerAsync(azuriteContainer, cancellationToken);
        return await Containers.SingleAsync(c => c.Name == dto.ContainerName, cancellationToken);
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
        ValidateContainerName(containerName);
        try
        {
            await azurite.DeleteContainerAsync(containerName, cancellationToken);
            await context.RemoveContainerAsync(containerName, cancellationToken);
        }
        catch (AzuriteServiceException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            // Ensure the container is removed from the cache even if it was not found in Azurite.
            await context.RemoveContainerAsync(containerName, cancellationToken);
        }
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
        ValidateContainerName(containerName);
        return await Containers.FirstOrDefaultAsync(c => c.Name == containerName, cancellationToken);
    }

    /// <summary>
    /// Updates an existing container in Azurite and updates the cache.
    /// </summary>
    /// <param name="dto">The container properties to update.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The updated container DTO.</returns>
    public async Task<ContainerDTO> UpdateContainerAsync(UpdateContainerDTO dto, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UpdateContainerAsync('{containerProps}') called", JsonSerializer.Serialize(dto));
        ValidateContainerName(dto.ContainerName);

        var properties = new AzuriteContainerProperties
        {
            Metadata = dto.Metadata
        };
        var azuriteContainer = await azurite.UpdateContainerAsync(dto.ContainerName, properties, cancellationToken);
        await context.UpsertContainerAsync(azuriteContainer, cancellationToken);

        // Single is ok here because we've done an Upsert on the database.
        return await Containers.SingleAsync(c => c.Name == dto.ContainerName, cancellationToken);
    }
    #endregion

    #region Upload and Download
    /// <summary>
    /// The queryable collection of upload sessions.
    /// </summary>
    public IQueryable<UploadDTO> Uploads
    {
        get => context.Uploads.Select(upload => new UploadDTO
        {
            Id = upload.UploadId,
            ContainerName = upload.ContainerName,
            Name = upload.BlobName,
            LastActivityAt = upload.LastActivityAt,
            Progress = upload.ContentLength > 0
                ? (upload.Blocks.Sum(b => b.BlockSize) / (double)upload.ContentLength) * 100.0
                : 0.0
        });
    }

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
    public async Task<BlobDownloadDTO> DownloadBlobAsync(string containerName, string blobName, string? httpRange = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("DownloadBlobAsync('{containerName}', '{blobName}', '{httpRange}') called", containerName, blobName, httpRange);

        var blobInfo = await GetBlobAsync(containerName, blobName, cancellationToken)
            ?? throw new ResourceNotFoundException($"Blob '{blobName}' in container '{containerName}' not found.") { ResourceName = $"{containerName}/{blobName}" };
        var azuriteResult = await azurite.DownloadBlobAsync(containerName, blobName, httpRange, cancellationToken);

        var result = new BlobDownloadDTO
        {
            Name = blobName,
            ContainerName = containerName,
            Content = azuriteResult.Content,
            ContentEncoding = blobInfo.ContentEncoding,
            ContentLanguage = blobInfo.ContentLanguage,
            ContentLength = blobInfo.ContentLength,
            ContentRange = azuriteResult.ContentRange,
            ContentType = blobInfo.ContentType,
            ETag = blobInfo.ETag,
            LastModified = blobInfo.LastModified,
            StatusCode = azuriteResult.StatusCode
        };

        if (azuriteResult.IsSuccess)
        {
            return result;
        }

        DisposeDownloadStream(result);
        throw new AzuriteServiceException($"Failed to download blob '{blobName}' from container '{containerName}'.")
        {
            StatusCode = azuriteResult.StatusCode
        };
    }

    /// <summary>
    /// Cancels an upload session and deletes all associated blocks.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the upload is cancelled.</returns>
    public async Task CancelUploadAsync(Guid uploadId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("CancelUploadAsync('{uploadId}') called", uploadId);
        var upload = await context.Uploads.FirstOrDefaultAsync(u => u.UploadId == uploadId, cancellationToken);
        if (upload is null)
        {
            logger.LogDebug("Upload session '{uploadId}' not found; nothing to cancel", uploadId);
            return;
        }

        context.Uploads.Remove(upload);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Cancelled upload session '{uploadId}' for blob '{blobName}' in container '{containerName}'", uploadId, upload.BlobName, upload.ContainerName);
    }

    /// <summary>
    /// Commits an upload session by assembling the blocks into a blob.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="blockIds">The ordered list of blocks to commit.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created blob DTO.</returns>
    public async Task<BlobDTO> CommitUploadAsync(Guid uploadId, IEnumerable<string> blockIds, CancellationToken cancellationToken = default)
    {
        var blockList = blockIds.ToList(); // This ensures the block list is enumerated only once.
        logger.LogDebug("CommitUploadAsync('{uploadId}', '{blockList}') called", uploadId, JsonSerializer.Serialize(blockList));

        // Get the upload session with its blocks
        var upload = await context.Uploads.Include(u => u.Blocks).FirstOrDefaultAsync(u => u.UploadId == uploadId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Upload session '{uploadId}' not found.") { ResourceName = uploadId.ToString() };

        // Validate that all blocks in the block list have been uploaded.
        var uploadedBlockIds = upload.Blocks.Select(b => b.BlockId).ToHashSet();
        var missingBlocks = blockList.Where(blockId => !uploadedBlockIds.Contains(blockId)).ToList();
        if (missingBlocks.Count > 0)
        {
            throw new AzuriteServiceException($"Cannot commit upload session '{uploadId}': missing blocks: {string.Join(", ", missingBlocks)}")
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        // Commit the upload to Azurite, returning an AzuriteBlobItem
        var properties = new AzuriteBlobProperties
        {
            ContentEncoding = upload.ContentEncoding,
            ContentLanguage = upload.ContentLanguage,
            ContentType = upload.ContentType,
            Metadata = upload.Metadata,
            Tags = upload.Tags
        };
        var commitResult = await azurite.UploadCommitAsync(
            upload.ContainerName,
            upload.BlobName,
            blockList,
            properties,
            cancellationToken
        );

        // Update the cache database.
        var result = await context.UpsertBlobAsync(commitResult, upload.ContainerName, cancellationToken);

        // Delete the upload session after a successful commit
        context.Uploads.Remove(upload);
        await context.SaveChangesAsync(cancellationToken);

        // Return the resulting BlobDTO.
        return await Blobs.SingleAsync(b => b.ContainerName == upload.ContainerName && b.Name == upload.BlobName, cancellationToken);
    }

    /// <summary>
    /// Initiates a chunked blob upload session.
    /// </summary>
    /// <param name="uploadDto">The upload request details.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The created upload session details.</returns>
    public async Task<UploadStatusDTO> CreateUploadAsync(CreateUploadRequestDTO uploadDto, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("CreateUploadAsync({uploadDto}) called", JsonSerializer.Serialize(uploadDto));

        var containerExists = await context.Containers.AnyAsync(c => c.Name == uploadDto.ContainerName, cancellationToken);
        if (!containerExists)
        {
            throw new ResourceNotFoundException($"Container '{uploadDto.ContainerName}' not found.") { ResourceName = uploadDto.ContainerName };
        }

        var blobExists = await context.Blobs.AnyAsync(b => b.ContainerName == uploadDto.ContainerName && b.Name == uploadDto.BlobName, cancellationToken);
        if (blobExists)
        {
            throw new ResourceExistsException($"Blob '{uploadDto.BlobName}' already exists in container '{uploadDto.ContainerName}'.") { ResourceName = $"{uploadDto.ContainerName}/{uploadDto.BlobName}" };
        }

        var uploadModel = new UploadModel
        {
            UploadId = Guid.NewGuid(),
            BlobName = uploadDto.BlobName,
            ContainerName = uploadDto.ContainerName,
            ContentEncoding = uploadDto.ContentEncoding,
            ContentLanguage = uploadDto.ContentLanguage,
            ContentLength = uploadDto.ContentLength,
            ContentType = uploadDto.ContentType,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow,
            Metadata = uploadDto.Metadata,
            Tags = uploadDto.Tags
        };

        context.Uploads.Add(uploadModel);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created upload session '{uploadId}' for blob '{blobName}' in container '{containerName}'", uploadModel.UploadId, uploadDto.BlobName, uploadDto.ContainerName);
        return await GetUploadStatusAsync(uploadModel.UploadId, cancellationToken);
    }

    /// <summary>
    /// Retrieves the status of an upload session.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>The upload status, or null if not found.</returns>
    public async Task<UploadStatusDTO> GetUploadStatusAsync(Guid uploadId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GetUploadStatusAsync('{uploadId}') called", uploadId);

        var upload = await context.Uploads.Include(u => u.Blocks).FirstOrDefaultAsync(u => u.UploadId == uploadId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Upload session '{uploadId}' not found.") { ResourceName = uploadId.ToString() };

        return new UploadStatusDTO
        {
            UploadId = upload.UploadId,
            ContainerName = upload.ContainerName,
            BlobName = upload.BlobName,
            ContentLength = upload.ContentLength,
            ContentType = upload.ContentType,
            UploadedBlocks = [.. upload.Blocks.Select(b => b.BlockId)],
            UploadedLength = upload.Blocks.Sum(b => b.BlockSize),
            CreatedAt = upload.CreatedAt,
            LastActivityAt = upload.LastActivityAt
        };
    }

    /// <summary>
    /// Uploads a block (chunk) to an in-progress upload session.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="blockId">The Base64-encoded block identifier.</param>
    /// <param name="content">The block content stream.</param>
    /// <param name="contentMD5">Optional MD5 hash for integrity verification.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the block is uploaded.</returns>
    public async Task UploadBlockAsync(Guid uploadId, string blockId, Stream content, string? contentMD5 = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UploadBlockAsync('{uploadId}', '{blockId}', content.Length={contentLength}) called", uploadId, blockId, content.Length);

        // Validate blockId format (must be base64)
        ValidateBlockId(blockId);

        // Get upload session
        var upload = await context.Uploads.Include(u => u.Blocks).FirstOrDefaultAsync(u => u.UploadId == uploadId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Upload session '{uploadId}' not found.") { ResourceName = uploadId.ToString() };

        // Upload block to Azurite - note that we do not pass contentMD5 to Azurite as we don't support it yet.
        var blockInfo = await azurite.UploadBlockAsync(
            upload.ContainerName,
            upload.BlobName,
            blockId,
            content,
            cancellationToken
        );
        if (!blockInfo.IsSuccess)
        {
            throw new AzuriteServiceException($"Failed to upload block '{blockId}' for upload session '{uploadId}'.")
            {
                StatusCode = blockInfo.StatusCode
            };
        }

        var blockModel = new UploadBlockModel
        {
            UploadId = uploadId,
            BlockId = blockId,
            BlockSize = content.Length,
            ContentMD5 = blockInfo.ContentMD5,
            UploadedAt = DateTimeOffset.UtcNow
        };
        await context.UpsertUploadBlockAsync(blockModel, cancellationToken);
        logger.LogInformation("Uploaded block '{blockId}' for upload session '{uploadId}'", blockId, uploadId);
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

    /// <summary>
    /// Disposes the content stream of the given <see cref="BlobDownloadDTO"/>.
    /// </summary>
    /// <param name="dto">The DTO to modify.</param>
    internal static void DisposeDownloadStream(BlobDownloadDTO dto)
    {
        try
        {
            dto.Content?.Dispose();
            dto.Content = null;
        }
        catch (Exception)
        {
            // Swallow exceptions during dispose to avoid masking original errors.
        }
    }

    /// <summary>
    /// Validates the provided block ID meets Azure Blob Storage requirements.
    /// </summary>
    /// <param name="blockId">The ID of the block.</param>
    /// <exception cref="AzuriteServiceException">Thrown if the blockId is invalid.</exception>
    internal static void ValidateBlockId(string blockId)
    {
        try
        {
            var decoded = Convert.FromBase64String(blockId);
            if (decoded.Length > 64)
            {
                throw new AzuriteServiceException($"Block ID '{blockId}' exceeds maximum length of 64 bytes when decoded.")
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }
        catch (FormatException)
        {
            throw new AzuriteServiceException($"Invalid block ID format: '{blockId}'. Block ID must be a valid Base64-encoded string.")
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }

    /// <summary>
    /// Throws an exception if the blob name is not valid.
    /// </summary>
    /// <param name="blobName">The name of the blob to validate.</param>
    /// <exception cref="AzuriteServiceException">Thrown if the blob name is invalid.</exception>
    internal static void ValidateBlobName(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new AzuriteServiceException("Blob name must be provided.") { StatusCode = StatusCodes.Status400BadRequest };
        }
    }

    /// <summary>
    /// Throws an exception if the container name is not valid.
    /// </summary>
    /// <param name="containerName">The name of the container to validate.</param>
    /// <exception cref="AzuriteServiceException">Thrown if the container name is invalid.</exception>
    internal static void ValidateContainerName(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new AzuriteServiceException("Container name must be provided.") { StatusCode = StatusCodes.Status400BadRequest };
        }
    }
}