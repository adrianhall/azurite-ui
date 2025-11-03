using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object for initiating a chunked blob upload.
/// </summary>
public class CreateUploadRequestDTO
{
    /// <summary>
    /// The name of the blob to upload.
    /// </summary>
    [property: Required]
    [property: Description("The name of the blob to upload")]
    public required string BlobName { get; set; }

    /// <summary>
    /// The name of the container to upload the blob to.
    /// </summary>
    [property: Required]
    [property: Description("The name of the container to upload the blob to")]
    public required string ContainerName { get; set; }

    /// <summary>
    /// The total expected content length in bytes.
    /// </summary>
    [property: Required]
    [property: Range(1, 10_737_418_240)] // 10GB max
    [property: Description("The total expected content length in bytes")]
    public long ContentLength { get; set; }

    /// <summary>
    /// The MIME type of the blob.
    /// </summary>
    [property: Description("The MIME type of the blob")]
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// The content encoding of the blob.
    /// </summary>
    [property: Description("The content encoding of the blob")]
    public string? ContentEncoding { get; set; }

    /// <summary>
    /// The content language of the blob.
    /// </summary>
    [property: Description("The content language of the blob")]
    public string? ContentLanguage { get; set; }

    /// <summary>
    /// The metadata to be applied to the blob upon commit.
    /// </summary>
    [property: Description("The metadata (key-value pairs) to be applied to the blob upon commit")]
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The tags to be applied to the blob upon commit.
    /// </summary>
    [property: Description("The tags (key-value pairs) to be applied to the blob upon commit")]
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}
