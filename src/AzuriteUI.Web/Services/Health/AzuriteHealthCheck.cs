using AzuriteUI.Web.Services.Azurite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AzuriteUI.Web.Services.Health;

/// <summary>
/// The health check for the Azurite Service.  This integrates with the
/// ASP.NET Core health checks.
/// </summary>
public class AzuriteHealthCheck(IAzuriteService service) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthStatus = await service.GetHealthStatusAsync(cancellationToken);
        Dictionary<string, object> data = new() { ["ConnectionString"] = healthStatus.ConnectionString };
        if (healthStatus.IsHealthy)
        {
            data["ResponseTime"] = healthStatus.ResponseTimeMilliseconds ?? 0L;
            return HealthCheckResult.Healthy("Healthy", data: data);
        }
        else
        {
            data["ErrorMessage"] = healthStatus.ErrorMessage ?? "Unknown error";
            return HealthCheckResult.Unhealthy("Service is unresponsive", data: data);
        }
    }
}