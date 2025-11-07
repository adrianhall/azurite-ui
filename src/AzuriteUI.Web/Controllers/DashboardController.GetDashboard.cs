using AzuriteUI.Web.Controllers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;

namespace AzuriteUI.Web.Controllers;

public partial class DashboardController : ODataController
{
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
