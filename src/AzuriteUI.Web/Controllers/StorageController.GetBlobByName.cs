using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    /// <summary>
    /// Retrieves the properties of a specific blob by its name.
    /// </summary>
    /// <remarks>
    /// This endpoint supports RFC 7232 conditional requests using ETag and Last-Modified headers.
    /// </remarks>
    /// <param name="containerName">The name of the container to retrieve.</param>
    /// <param name="blobName">The name of the blob to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The container properties if found; otherwise, a NotFound result.</returns>
    [HttpGet("{containerName}/blobs/{blobName}")]
    [EndpointName("GetBlobByName")]
    [EndpointDescription("Retrieves the properties of a specific blob by its name.")]
    [ProducesResponseType<BlobDTO>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public virtual async Task<IActionResult> GetBlobByNameAsync(
        [FromRoute] string containerName,
        [FromRoute] string blobName,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("GetBlobByNameAsync({containerName}, {blobName})", containerName, blobName);

        BlobDTO? blob = await Repository.GetBlobAsync(containerName, blobName, cancellationToken);
        if (blob is null)
        {
            return NotFound();
        }

        int? statusCode = GetResponseForConditionalRequest(blob);
        return statusCode.HasValue ? ConditionalResponse(statusCode.Value, blob) : Ok(blob);
    }
}