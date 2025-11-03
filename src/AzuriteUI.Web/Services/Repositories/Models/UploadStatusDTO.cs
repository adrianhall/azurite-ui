using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object for upload status information.
/// </summary>
public class UploadStatusDTO
{
    /// <summary>
    /// The unique identifier for the upload session.
    /// </summary>
    [property: Required]
    [property: Description("The unique identifier for the upload session")]
    public required Guid UploadId { get; set; }

    /// <summary>
    /// The name of the container where the blob will be created.
    /// </summary>
    [property: Required]
    [property: Description("The name of the container where the blob will be created")]
    public required string ContainerName { get; set; }

    /// <summary>
    /// The name of the blob being uploaded.
    /// </summary>
    [property: Required]
    [property: Description("The name of the blob being uploaded")]
    public required string BlobName { get; set; }

    /// <summary>
    /// The total expected content length in bytes.
    /// </summary>
    [property: Description("The total expected content length in bytes")]
    public long ContentLength { get; set; }

    /// <summary>
    /// The MIME type of the blob.
    /// </summary>
    [property: Description("The MIME type of the blob")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The list of block IDs that have been successfully uploaded.
    /// </summary>
    [property: Required]
    [property: Description("The list of block IDs that have been successfully uploaded")]
    public List<string> UploadedBlocks { get; set; } = [];

    /// <summary>
    /// The total number of bytes that have been uploaded across all blocks.
    /// </summary>
    [property: Required]
    [property: Description("The total number of bytes that have been uploaded across all blocks")]
    public long UploadedLength { get; set; }

    /// <summary>
    /// The date/time when the upload was initiated.
    /// </summary>
    [property: Required]
    [property: Description("The date/time when the upload was initiated")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date/time of the last activity (upload, status check, etc.).
    /// </summary>
    [property: Required]
    [property: Description("The date/time of the last activity")]
    public DateTimeOffset LastActivityAt { get; set; }
}