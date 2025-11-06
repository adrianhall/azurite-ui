using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;

namespace AzuriteUI.Web.Controllers;

public partial class UploadsController : ODataController
{
    /// <summary>
    /// Retrieves the status of an upload session, including progress and uploaded blocks.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The upload session status.</returns>
    [HttpGet("{uploadId:guid}")]
    [EndpointName("GetUploadStatus")]
    [EndpointDescription("Retrieves the status of an upload session, including progress and uploaded blocks.")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType<UploadStatusDTO>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> GetUploadStatusAsync(
        [FromRoute] Guid uploadId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("GetUploadStatusAsync('{uploadId}') called", uploadId);

        UploadStatusDTO uploadStatus = await Repository.GetUploadStatusAsync(uploadId, cancellationToken);
        return Ok(uploadStatus);
    }
}
