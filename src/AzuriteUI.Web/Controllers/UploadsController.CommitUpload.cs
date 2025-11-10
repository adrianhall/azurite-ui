using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;
using System.Text.Json;

namespace AzuriteUI.Web.Controllers;

public partial class UploadsController : ODataController
{
    /// <summary>
    /// Commits an upload session by finalizing the blob with the specified block list.
    /// </summary>
    /// <remarks>
    /// This operation will create the blob in Azure Storage and remove the upload session.
    /// </remarks>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="request">The commit request containing the ordered list of block IDs.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The created blob details.</returns>
    [HttpPut("{uploadId:guid}/commit")]
    [EndpointName("CommitUpload")]
    [EndpointDescription("Commits an upload session by finalizing the blob with the specified block list.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType<BlobDTO>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<IActionResult> CommitUploadAsync(
        [FromRoute] Guid uploadId,
        [FromBody] CommitUploadRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("CommitUploadAsync(uploadId: '{uploadId}', blockIds: '{blockIds}') called",
            uploadId, JsonSerializer.Serialize(request.BlockIds));

        BlobDTO blob = await Repository.CommitUploadAsync(uploadId, request.BlockIds, cancellationToken);

        Logger.LogInformation("Upload '{uploadId}' committed successfully. Blob '{containerName}/{blobName}' created.",
            uploadId, blob.ContainerName, blob.Name);

        // Set Location header pointing to the blob resource
        Response.Headers.Location = $"/api/containers/{blob.ContainerName}/blobs/{blob.Name}";

        return Ok(blob);
    }
}
