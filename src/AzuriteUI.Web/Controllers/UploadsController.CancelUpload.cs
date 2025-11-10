using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AzuriteUI.Web.Controllers;

public partial class UploadsController : ODataController
{
    /// <summary>
    /// Cancels an upload session and removes it from the cache.
    /// </summary>
    /// <remarks>
    /// Staged blocks in Azure Storage will automatically expire.
    /// </remarks>
    /// <param name="uploadId">The unique identifier of the upload session to cancel.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>No content on successful cancellation.</returns>
    [HttpDelete("{uploadId:guid}")]
    [EndpointName("CancelUpload")]
    [EndpointDescription("Cancels an upload session and removes it from the cache.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> CancelUploadAsync(
        [FromRoute] Guid uploadId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("CancelUploadAsync('{uploadId}') called", uploadId);

        await Repository.CancelUploadAsync(uploadId, cancellationToken);

        Logger.LogInformation("Upload session '{uploadId}' cancelled successfully", uploadId);

        return NoContent();
    }
}
