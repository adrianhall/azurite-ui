using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    /// <summary>
    /// Deletes a specific blob by its name.
    /// </summary>
    /// <param name="containerName">The name of the container containing the blob.</param>
    /// <param name="blobName">The name of the blob to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpDelete("{containerName}/blobs/{blobName}")]
    [EndpointName("DeleteBlob")]
    [EndpointDescription("Deletes a specific blob by its name.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public virtual async Task<IActionResult> DeleteBlobAsync(
        [FromRoute] string containerName,
        [FromRoute] string blobName,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("DeleteBlobAsync({containerName}, {blobName})", containerName, blobName);

        BlobDTO? blob = await Repository.GetBlobAsync(containerName, blobName, cancellationToken);
        if (blob is null)
        {
            Logger.LogInformation("DeleteBlobAsync: Blob '{blobName}' not found in container '{containerName}'", blobName, containerName);
            return NotFound();
        }

        int? statusCode = GetResponseForConditionalRequest(blob);
        if (statusCode.HasValue)
        {
            Logger.LogInformation("DeleteBlobAsync: Conditional request for blob '{blobName}' in container '{containerName}' resulted in {StatusCode}", blobName, containerName, statusCode.Value);
            return ConditionalResponse(statusCode.Value, blob);
        }

        try
        {
            Logger.LogInformation("DeleteBlobAsync: Deleting blob '{blobName}' in container '{containerName}'", blobName, containerName);
            await Repository.DeleteBlobAsync(containerName, blobName, cancellationToken);
            return NoContent();
        }
        catch (AzuriteServiceException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            Logger.LogInformation("DeleteBlobAsync: Blob '{blobName}' not found in container '{containerName}' during deletion", blobName, containerName);
            return NoContent();
        }
    }
}
