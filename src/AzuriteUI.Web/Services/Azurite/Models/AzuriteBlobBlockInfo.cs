namespace AzuriteUI.Web.Services.Azurite.Models;

/// <summary>
/// The  result from uploading a block blob to Azurite.
/// </summary>
public class AzuriteBlobBlockInfo
{
    /// <summary>
    /// The block ID for this chunk. Must be Base64-encoded and unique within the blob.
    /// </summary>
    public required string BlockId { get; set; }

    /// <summary>
    /// The MD5 hash of the chunk content, used for integrity verification.
    /// </summary>
    public string? ContentMD5 { get; set; }

    /// <summary>
    /// The HTTP Status code of the upload chunk operation.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Indicates whether the upload chunk operation was successful.
    /// </summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode <= 299;
}
