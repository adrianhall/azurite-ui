using System.ComponentModel;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object representing an update operation on a blob in Azurite.
/// </summary>
public class BlobUpdateDTO
{
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
    /// The metadata for the blob.
    /// </summary>
    [property: Description("The metadata (key-value pairs) for the blob")]
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The tags (key-value pairs) associated with this blob.
    /// </summary>
    [property: Description("The tags (key-value pairs) associated with this blob")]
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}