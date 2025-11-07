using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Controllers.Models;

/// <summary>
/// Response for the dashboard endpoint.
/// </summary>
public class DashboardResponse
{
    /// <summary>
    /// Statistics for the dashboard.
    /// </summary>
    [property: Required]
    [property: Description("Statistics for the dashboard")]
    public required DashboardStats Stats { get; set; }

    /// <summary>
    /// The most recently modified containers (up to 10).
    /// </summary>
    [property: Required]
    [property: Description("The most recently modified containers (up to 10)")]
    public required IEnumerable<RecentContainerInfo> RecentContainers { get; set; }

    /// <summary>
    /// The most recently modified blobs (up to 10).
    /// </summary>
    [property: Required]
    [property: Description("The most recently modified blobs (up to 10)")]
    public required IEnumerable<RecentBlobInfo> RecentBlobs { get; set; }
}
