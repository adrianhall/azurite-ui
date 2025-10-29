using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Services.CacheDb.Models;

/// <summary>
/// A model representing an in-progress chunked blob upload.
/// </summary>
public class UploadModel
{
    /// <summary>
    /// The unique identifier for this upload session.
    /// </summary>
    public required Guid UploadId { get; set; }

    /// <summary>
    /// The name of the container where the blob will be created.
    /// </summary>
    public required string ContainerName { get; set; }

    /// <summary>
    /// The name of the blob being uploaded.
    /// </summary>
    public required string BlobName { get; set; }

    /// <summary>
    /// The total expected content length in bytes.
    /// </summary>
    public long ContentLength { get; set; }

    /// <summary>
    /// The MIME type of the blob.
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// The content encoding of the blob.
    /// </summary>
    public string? ContentEncoding { get; set; }

    /// <summary>
    /// The content language of the blob.
    /// </summary>
    public string? ContentLanguage { get; set; }

    /// <summary>
    /// The metadata to be applied to the blob upon commit.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The tags to be applied to the blob upon commit.
    /// </summary>
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The date/time when the upload was initiated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date/time of the last activity (upload, status check, etc.).
    /// </summary>
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation property for the uploaded blocks.
    /// </summary>
    public virtual ICollection<UploadBlockModel> Blocks
    {
        get;
        [ExcludeFromCodeCoverage(Justification = "Set by EF Core so not tested")]
        set;
    } = [];
}
