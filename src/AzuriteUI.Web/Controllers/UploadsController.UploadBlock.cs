using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;

namespace AzuriteUI.Web.Controllers;

public partial class UploadsController : ODataController
{
    /// <summary>
    /// Uploads a block (chunk) of data for an upload session. The block data is streamed
    /// directly to Azure Storage without buffering in memory.
    /// </summary>
    /// <param name="uploadId">The unique identifier of the upload session.</param>
    /// <param name="blockId">The Base64-encoded block identifier (must be unique within the blob and max 64 bytes when decoded).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>Success response when block is uploaded.</returns>
    [HttpPut("{uploadId:guid}/blocks/{blockId}")]
    [EndpointName("UploadBlock")]
    [EndpointDescription("Uploads a block (chunk) of data for an upload session.")]
    [Consumes("application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10_737_418_240)] // 10 GB max
    public virtual async Task<IActionResult> UploadBlockAsync(
        [FromRoute] Guid uploadId,
        [FromRoute] string blockId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("UploadBlockAsync(uploadId: '{uploadId}', blockId: '{blockId}') called", uploadId, blockId);

        // Get Content-MD5 header if provided
        string? contentMD5 = Request.Headers.TryGetValue("Content-MD5", out var md5Value)
            ? md5Value.ToString()
            : null;

        // Create a seekable stream from the request body
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Seek(0, SeekOrigin.Begin);
        
        // The repository will handle validation and pass-through to Azurite
        await Repository.UploadBlockAsync(uploadId, blockId, memoryStream, contentMD5, cancellationToken);

        Logger.LogInformation("Block '{blockId}' uploaded successfully for upload '{uploadId}'", blockId, uploadId);

        return Ok(new
        {
            uploadId,
            blockId,
            message = "Block uploaded successfully"
        });
    }
}
