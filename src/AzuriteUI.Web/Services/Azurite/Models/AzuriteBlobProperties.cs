namespace AzuriteUI.Web.Services.Azurite.Models;

/// <summary>
/// The properties of an Azurite Blob that can be updated or provided
/// when uploading a blob.
/// </summary>
public class AzuriteBlobProperties
{ 
    /// <summary>
    /// The content encoding of the blob.
    /// </summary>
    public string? ContentEncoding { get; set; }

    /// <summary>
    /// The content language of the blob.
    /// </summary>
    public string? ContentLanguage { get; set; }

    /// <summary>
    /// The content type of the blob.
    /// </summary>
    public string? ContentType { get; set; }
    
    /// <summary>
    /// The list of custom metadata key-value pairs to set for the container.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The tags (key-value pairs) associated with this blob.
    /// </summary>
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

}
