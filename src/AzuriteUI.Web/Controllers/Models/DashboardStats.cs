using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Controllers.Models;

/// <summary>
/// Statistics for the dashboard.
/// </summary>
public class DashboardStats
{
    /// <summary>
    /// The total number of containers.
    /// </summary>
    [property: Required]
    [property: Description("The total number of containers")]
    public required int Containers { get; set; }

    /// <summary>
    /// The total number of blobs.
    /// </summary>
    [property: Required]
    [property: Description("The total number of blobs")]
    public required int Blobs { get; set; }

    /// <summary>
    /// The total size of all blobs in bytes.
    /// </summary>
    [property: Required]
    [property: Description("The total size of all blobs in bytes")]
    public required long TotalBlobSize { get; set; }

    /// <summary>
    /// The total size of all image blobs in bytes.
    /// </summary>
    [property: Required]
    [property: Description("The total size of all image blobs in bytes")]
    public required long TotalImageSize { get; set; }
}
