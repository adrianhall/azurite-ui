using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AzuriteUI.Web.Pages.Shared.Components;

/// <summary>
/// View component for displaying the connection status indicator.
/// Shows whether the Azurite service is healthy (connected) or unhealthy (disconnected).
/// </summary>
/// <param name="healthCheckService">The health check service to query system health.</param>
public class StatusIndicatorViewComponent(HealthCheckService healthCheckService) : ViewComponent
{
    /// <summary>
    /// Invokes the status indicator view component.
    /// </summary>
    /// <returns>The view component result with the health status.</returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var healthReport = await healthCheckService.CheckHealthAsync(HttpContext.RequestAborted);
        var isHealthy = healthReport.Status == HealthStatus.Healthy;

        var model = new StatusIndicatorViewModel
        {
            IsHealthy = isHealthy,
            StatusText = isHealthy ? "Connected" : "Disconnected",
            Icon = isHealthy ? "wifi" : "wifi-off",
            CssClass = isHealthy ? "connected" : "disconnected"
        };

        return View(model);
    }
}

/// <summary>
/// View model for the status indicator component.
/// </summary>
public class StatusIndicatorViewModel
{
    /// <summary>
    /// Gets or sets whether the system is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the status text to display (e.g., "Connected" or "Disconnected").
    /// </summary>
    public required string StatusText { get; set; }

    /// <summary>
    /// Gets or sets the Bootstrap icon name to display.
    /// </summary>
    public required string Icon { get; set; }

    /// <summary>
    /// Gets or sets the CSS class for styling the status indicator.
    /// </summary>
    public required string CssClass { get; set; }
}
