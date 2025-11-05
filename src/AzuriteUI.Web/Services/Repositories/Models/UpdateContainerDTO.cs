using System.ComponentModel;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object for creating or updating a container in Azurite.
/// Contains only the settable fields.
/// </summary>
public class UpdateContainerDTO
{
    /// <summary>
    /// The name of the container to create.
    /// </summary>
    [property: Description("The name of the container to create")]
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// The metadata for the container.
    /// </summary>
    [property: Description("The metadata (key-value pairs) for the container")]
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}