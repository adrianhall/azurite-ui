using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object representing a downloaded blob from Azurite.
/// </summary>
public class BlobDownloadDTO
{
    /// <summary>
    /// The name of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The name of the blob")]
    public required string Name { get; set; }

    /// <summary>
    /// The name of the container that contains the blob.
    /// </summary>
    [property: Required]
    [property: Description("The name of the container that contains the blob")]
    public required string ContainerName { get; set; }

    /// <summary>
    /// The entity tag of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The entity tag of the blob")]
    public required string ETag { get; set; }

    /// <summary>
    /// The date/time that the blob was last modified.
    /// </summary>
    [property: Required]
    [property: Description("The date/time that the blob was last modified")]
    public required DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// The content stream of the downloaded blob.
    /// </summary>
    /// <remarks>
    /// The stream should be disposed by the caller when no longer needed.
    /// </remarks>
    [property: Required]
    [property: Description("The content stream of the downloaded blob")]
    public Stream? Content { get; set; }

    /// <summary>
    /// The content encoding of the blob.
    /// </summary>
    [property: Description("The content encoding of the blob")]
    public string ContentEncoding { get; set; } = string.Empty;

    /// <summary>
    /// The content language of the blob.
    /// </summary>
    [property: Description("The content language of the blob")]
    public string ContentLanguage { get; set; } = string.Empty;

    /// <summary>
    /// The length of the blob content.
    /// </summary>
    [property: Required]
    [property: Description("The length of the blob content")]
    public long ContentLength { get; set; } = 0L;

    /// <summary>
    /// The content range returned for partial downloads.
    /// </summary>
    [property: Description("The content range returned for partial downloads")]
    public string? ContentRange { get; set; }

    /// <summary>
    /// The content type of the blob.
    /// </summary>
    [property: Required]
    [property: Description("The content type of the blob")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP status code of the download operation.
    /// </summary>
    /// <remarks>
    /// Possible values include:
    /// 200 - OK (full content)
    /// 206 - Partial Content (partial content)
    /// 400 - Bad Request (invalid range)
    /// 404 - Not Found (blob does not exist)
    /// 416 - Range Not Satisfiable (requested range not valid)
    /// </remarks>
    [property: Required]
    [property: Description("The HTTP status code of the download operation")]
    public int StatusCode { get; set; }
}