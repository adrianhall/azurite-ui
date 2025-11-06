using System.ComponentModel;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object representing an update operation on a blob in Azurite.
/// </summary>
public class UpdateBlobDTO
{
    /// <summary>
    /// The name of the container containing the blob.
    /// </summary>
    [property: Description("The name of the container containing the blob")]
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The name of the blob.
    /// </summary>
    [property: Description("The name of the blob")]
    public string BlobName { get; set; } = string.Empty;

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