using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object for creating or updating a container in Azurite.
/// Contains only the settable fields.
/// </summary>
public class ContainerUpdateDTO
{
    /// <summary>
    /// Specifies the default encryption scope for the container.
    /// </summary>
    [property: Description("The default encryption scope for the container")]
    public string DefaultEncryptionScope { get; set; } = string.Empty;

    /// <summary>
    /// The metadata for the container.
    /// </summary>
    [property: Description("The metadata (key-value pairs) for the container")]
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Specifies whether the container prevents overriding the encryption scope set at the container level.
    /// </summary>
    [property: Description("Specifies whether the container prevents overriding the encryption scope set at the container level")]
    public bool PreventEncryptionScopeOverride { get; set; } = false;

    /// <summary>
    /// Specifies whether data in the container may be accessed publicly and the level of access.
    /// </summary>
    [property: Description("Specifies whether data in the container may be accessed publicly and the level of access")]
    [property: RegularExpression("^(none|blob|blobcontainer)$", ErrorMessage = "PublicAccess must be one of: none, blob, blobcontainer")]
    public string PublicAccess { get; set; } = "none";
}