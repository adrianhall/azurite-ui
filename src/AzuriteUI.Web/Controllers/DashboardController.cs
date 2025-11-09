using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;

namespace AzuriteUI.Web.Controllers;

/// <summary>
/// The controller that manages the dashboard endpoint at <c>/api/dashboard</c>.
/// </summary>
/// <param name="repository">The storage repository to use for data access.</param>
/// <param name="logger">The logger to use for diagnostics and reporting.</param>
[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    IStorageRepository repository,
    ILogger<DashboardController> logger
) : ControllerBase
{
    /// <summary>
    /// The storage repository to use for data access.
    /// </summary>
    public IStorageRepository Repository => repository;

    /// <summary>
    /// The logger for diagnostics and reporting.
    /// </summary>
    public ILogger Logger => logger;

        /// <summary>
    /// Retrieves dashboard information including statistics about containers and blobs,
    /// and lists of recently modified containers and blobs.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The dashboard data.</returns>
    [HttpGet]
    [EndpointName("GetDashboard")]
    [EndpointDescription("Retrieves dashboard information including statistics and recently modified containers and blobs.")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("GetDashboardAsync() called");

        DashboardResponse response = await Repository.GetDashboardDataAsync(cancellationToken);
        return Ok(response);
    }
}
