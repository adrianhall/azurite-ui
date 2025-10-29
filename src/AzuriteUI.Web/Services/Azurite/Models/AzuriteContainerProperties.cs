namespace AzuriteUI.Web.Services.Azurite.Models;

/// <summary>
/// The properties of an Azurite Container that can be updated or provided
/// when creating a container.
/// </summary>
public class AzuriteContainerProperties
{
    /// <summary>
    /// Optional parameter to specify the public access type of the container.
    /// </summary>
    public AzuritePublicAccess? PublicAccessType { get; set; }

    /// <summary>
    /// The list of custom metadata key-value pairs to set for the container.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// If set, specifies the default encryption scope for the container.
    /// </summary>
    public string? DefaultEncryptionScope { get; set; }

    /// <summary>
    /// If set, specifies whether to prevent overriding the encryption scope.
    /// </summary>
    public bool? PreventEncryptionScopeOverride { get; set; }
}