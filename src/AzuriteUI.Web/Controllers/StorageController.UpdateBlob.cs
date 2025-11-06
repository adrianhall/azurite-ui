using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;
using System.Text.Json;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    /// <summary>
    /// Updates an existing blob with new metadata and tags.
    /// </summary>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobName">The name of the blob to update.</param>
    /// <param name="dto">The blob properties to set.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An <see cref="OkObjectResult"/> or <see cref="CreatedResult"/> response object with the container.</returns>
    [HttpPut("{containerName}/blobs/{blobName}")]
    [EndpointName("UpdateBlob")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<BlobDTO>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]  
    public virtual async Task<IActionResult> UpdateBlobAsync(
        [FromRoute] string containerName,
        [FromRoute] string blobName,
        [FromBody] UpdateBlobDTO dto,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("UpdateBlobAsync('{containerName}', '{blobName}', '{blobProps}') called", containerName, blobName, JsonSerializer.Serialize(dto));
        
        dto.ContainerName = dto.ContainerName.OrDefault(containerName);
        if (dto.ContainerName != containerName)
        {
            Logger.LogWarning("UpdateBlobAsync: Mismatch between route containerName '{RouteContainerName}' and body containerName '{BodyContainerName}'", containerName, dto.ContainerName);
            return BadRequest("Container name in the URL must match the container name in the request body.");
        }

        dto.BlobName = dto.BlobName.OrDefault(blobName);
        if (dto.BlobName != blobName)
        {
            Logger.LogWarning("UpdateBlobAsync: Mismatch between route blobName '{RouteBlobName}' and body blobName '{BodyBlobName}'", blobName, dto.BlobName);
            return BadRequest("Blob name in the URL must match the blob name in the request body.");
        }

        BlobDTO? existingBlob = await Repository.GetBlobAsync(dto.ContainerName, dto.BlobName, cancellationToken);
        if (existingBlob is null)
        {
            Logger.LogWarning("UpdateBlobAsync: Blob '{blobName}' not found in container '{containerName}'.", blobName, containerName);
            return NotFound();
        }

        int? statusCode = GetResponseForConditionalRequest(existingBlob);
        if (statusCode.HasValue)
        {
            Logger.LogInformation("UpdateBlobAsync: Conditional request for blob '{blobName}' in container '{containerName}' resulted in {StatusCode}", blobName, containerName, statusCode.Value);
            return ConditionalResponse(statusCode.Value, existingBlob);
        }

        BlobDTO updatedBlob = await Repository.UpdateBlobAsync(dto, cancellationToken);
        return Ok(updatedBlob);
    }
}