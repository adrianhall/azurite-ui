namespace AzuriteUI.Web.Services.Azurite.Models;

/// <summary>
/// The model used to represent the health status of an Azurite instance.
/// </summary>
public class AzuriteHealthStatus
{
    /// <summary>
    /// The connection string used to connect to the Azurite service.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// If true, the Azurite service is healthy and responsive.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// If healthy, the response time of the Azurite service in milliseconds.
    /// </summary>
    public long? ResponseTimeMilliseconds { get; set; }

    /// <summary>
    /// If not healthy, the error message describing the issue.
    /// </summary>
    public string? ErrorMessage { get; set; }
}