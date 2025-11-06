using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;
using System.Text.Json;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    /// <summary>
    /// Initiates a new blob upload session for chunked uploads.
    /// </summary>
    /// <param name="containerName">The name of the container where the blob will be uploaded.</param>
    /// <param name="dto">The upload request containing blob properties and metadata.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The upload session details including the upload ID.</returns>
    [HttpPost("{containerName}/blobs")]
    [EndpointName("CreateUpload")]
    [EndpointDescription("Initiates a new blob upload session for chunked uploads.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<UploadStatusDTO>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    public virtual async Task<IActionResult> CreateUploadAsync(
        [FromRoute] string containerName,
        [FromBody] CreateUploadRequestDTO dto,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("CreateUploadAsync('{containerName}', '{uploadRequest}') called",
            containerName, JsonSerializer.Serialize(dto));

        // Validate that the container name in the route matches the DTO
        if (!string.Equals(containerName, dto.ContainerName, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                error = "ContainerNameMismatch",
                message = "The container name in the route must match the container name in the request body."
            });
        }

        UploadStatusDTO result = await Repository.CreateUploadAsync(dto, cancellationToken);
        return CreatedAtAction(
            actionName: "GetUploadStatus",
            controllerName: "Uploads",
            routeValues: new { result.UploadId },
            value: result);
    }
}
