using System.Diagnostics.CodeAnalysis;

namespace AzuriteUI.Web.Services.CacheDb.Models;

/// <summary>
/// A model representing a single uploaded block (chunk) within an upload session.
/// </summary>
public class UploadBlockModel
{
    /// <summary>
    /// Auto-incrementing primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The upload session this block belongs to.
    /// </summary>
    public required Guid UploadId { get; set; }

    /// <summary>
    /// The Base64-encoded block ID provided by the client.
    /// </summary>
    public required string BlockId { get; set; }

    /// <summary>
    /// The size of this block in bytes.
    /// </summary>
    public long BlockSize { get; set; }

    /// <summary>
    /// The MD5 hash of the block content (if provided).
    /// </summary>
    public string? ContentMD5 { get; set; }

    /// <summary>
    /// The date/time when this block was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation property back to the upload session.
    /// </summary>
    public virtual UploadModel? Upload
    {
        get;
        [ExcludeFromCodeCoverage(Justification = "Set by EF Core so not tested")]
        set;
    }
}
