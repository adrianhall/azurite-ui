namespace AzuriteUI.Web.Services.Azurite.Models;

/// <summary>
/// The result from an Azurite download blob operation.
/// </summary>
public class AzuriteBlobDownloadResult
{
    /// <summary>
    /// The content of the downloaded blob.
    /// </summary>
    /// <remarks>
    /// The stream should be disposed by the caller when no longer needed.
    /// </remarks>
    public Stream? Content { get; set; }

    /// <summary>
    /// The length of the blob.
    /// </summary>
    public long? ContentLength { get; set; }

    /// <summary>
    /// For partial content, contains the range information.
    /// </summary>
    public string? ContentRange { get; set; }

    /// <summary>
    /// The content type of the blob.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The HTTP Status code of the download operation.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Indicates whether the download operation was successful.
    /// </summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode <= 299;
}
