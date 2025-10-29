using Azure.Storage.Blobs.Models;
using AzuriteUI.Web.Services.Azurite.Models;

namespace AzuriteUI.Web.Extensions;

/// <summary>
/// A set of extension methods for the Azure SDK.
/// </summary>
internal static class AzureExtensions
{
    /// <summary>
    /// Ensures that a string value is dequoted (removes leading and trailing quotes).  This
    /// is normally used for ETag de-quoting because the Azure SDK is not careful about this.
    /// </summary>
    /// <param name="value">The value to be dequoted.</param>
    /// <returns>The dequoted string.</returns>
    public static string Dequote(this string? value)
        => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : value.StartsWith('"') && value.EndsWith('"') ? value.Trim('"') : value;

    /// <summary>
    /// Converts an Azure SDK nullable BlobType? to an AzuriteBlobType.
    /// </summary>
    /// <param name="blobType">The blob type to convert.</param>
    /// <returns>The converted blob type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the blob type is unsupported.</exception>
    internal static AzuriteBlobType ToAzuriteBlobType(this BlobType? blobType)
    {
        return blobType switch
        {
            null => AzuriteBlobType.Block,
            BlobType.Block => AzuriteBlobType.Block,
            BlobType.Append => AzuriteBlobType.Append,
            BlobType.Page => AzuriteBlobType.Page,
            _ => throw new ArgumentOutOfRangeException(nameof(blobType), "Unsupported blob type.")
        };
    }

    /// <summary>
    /// Converts an Azure SDK PublicAccessType to an AzuritePublicAccess.
    /// </summary>
    /// <param name="publicAccess">The public access type to convert.</param>
    /// <returns>The converted public access type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the public access type is unsupported.</exception>
    internal static AzuritePublicAccess ToAzuritePublicAccess(this PublicAccessType publicAccess)
    {
        return publicAccess switch
        {
            PublicAccessType.None => AzuritePublicAccess.None,
            PublicAccessType.Blob => AzuritePublicAccess.Blob,
            PublicAccessType.BlobContainer => AzuritePublicAccess.Container,
            _ => throw new ArgumentOutOfRangeException(nameof(publicAccess), "Unsupported public access type.") 
        };
    }

    /// <summary>
    /// Converts the content hash from the Azurite service to an optional Base64 string.
    /// </summary>
    /// <param name="contentHash">The content hash</param>
    /// <returns>The ContentMD5 header value.</returns>
    internal static string? AsOptionalBase64(this byte[]? contentHash)
        => contentHash is null ? null : Convert.ToBase64String(contentHash);
}