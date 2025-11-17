using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    internal static readonly string[] ValidContentDispositions = ["attachment", "inline"];

    /// <summary>
    /// Downloads the content of a blob from the specified container.
    /// </summary>
    /// <remarks>
    /// Supports HTTP range requests, partial content, and content disposition.
    /// </remarks>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="blobName">The name of the blob to download.</param>
    /// <param name="disposition">Optional Content-Disposition value: "attachment" to force download, "inline" to display in browser.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the blob content stream.
    /// Returns 200 OK for full content, 206 Partial Content for range requests, 416 for Range Not Satisfiable.
    /// Note: always returns a FileStreamResult which disposes the stream after response completion.  Error
    /// conditions are handled by the exception filter.
    /// </returns>
    [HttpGet("{containerName}/blobs/{blobName}/content")]
    [EndpointName("DownloadBlob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
    public virtual async Task<IActionResult> DownloadBlobAsync(
        [FromRoute] string containerName,
        [FromRoute] string blobName,
        [FromQuery] string? disposition = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("DownloadBlobAsync(containerName: {ContainerName}, blob: {BlobName})", containerName, blobName);

        // Validate disposition parameter if provided
        if (disposition is not null && !ValidContentDispositions.Contains(disposition, StringComparer.OrdinalIgnoreCase))
        {
            Logger.LogWarning("Invalid disposition value '{Disposition}' provided for blob '{ContainerName}/{BlobName}'.", disposition, containerName, blobName);
            return BadRequest($"Invalid disposition value. Valid values are: {string.Join(", ", ValidContentDispositions)}");
        }

        // Get the Range header from the request
        string? rangeHeader = Request.Headers.TryGetValue(HeaderNames.Range, out Microsoft.Extensions.Primitives.StringValues value) ? value.ToString() : null;
        BlobDownloadDTO downloadResult = await Repository.DownloadBlobAsync(containerName, blobName, rangeHeader, cancellationToken);

        // Set response headers
        Response.Headers.AcceptRanges = "bytes";

        // Set Content-Disposition header if disposition parameter is provided
        if (disposition is not null)
        {
            // Use ContentDispositionHeaderValue to properly format the header
            // Note: ContentDispositionHeaderValue automatically handles RFC 5987 encoding for FileNameStar
            var contentDisposition = new ContentDispositionHeaderValue(disposition.ToLowerInvariant())
            {
                FileName = blobName,
                FileNameStar = blobName
            };
            Response.Headers.ContentDisposition = contentDisposition.ToString();
        }

        // Transfer ownership of the stream to FileStreamResult
        // FileStreamResult will dispose the stream when the response is complete
        var fileResult = new FileStreamResult(downloadResult.Content!, downloadResult.ContentType)
        {
            EnableRangeProcessing = false, // We're handling ranges ourselves
            EntityTag = ConvertToEntityTagHeaderValue(downloadResult.ETag),
            LastModified = downloadResult.LastModified
        };

        // For 206 Partial Content, set the Content-Range header and status code
        if (downloadResult.StatusCode == StatusCodes.Status206PartialContent && !string.IsNullOrEmpty(downloadResult.ContentRange))
        {
            Response.Headers.ContentRange = downloadResult.ContentRange;
            Response.StatusCode = StatusCodes.Status206PartialContent;
            Logger.LogInformation("Blob '{ContainerName}/{BlobName}' partial content retrieved successfully. Range: {ContentRange}",
                containerName, blobName, downloadResult.ContentRange);
        }
        else
        {
            Logger.LogInformation("Blob '{ContainerName}/{BlobName}' full content retrieved successfully.", containerName, blobName);
        }

        return fileResult;
    }
    
    internal static EntityTagHeaderValue? ConvertToEntityTagHeaderValue(string? etag)
    {
        if (string.IsNullOrEmpty(etag))
        {
            return null;
        }

        // Ensure ETag is properly quoted
        if (!etag.StartsWith('"'))
        {
            etag = $"\"{etag}\"";
        }

        return new EntityTagHeaderValue(etag);
    }
}
