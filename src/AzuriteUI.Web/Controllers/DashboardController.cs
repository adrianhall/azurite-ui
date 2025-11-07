using AzuriteUI.Web.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AzuriteUI.Web.Controllers;

/// <summary>
/// The controller that manages the dashboard endpoint at <c>/api/dashboard</c>.
/// </summary>
/// <param name="repository">The storage repository to use for data access.</param>
/// <param name="logger">The logger to use for diagnostics and reporting.</param>
[ApiController]
[Route("api/dashboard")]
public partial class DashboardController(
    IStorageRepository repository,
    ILogger<DashboardController> logger
) : ODataController
{
    /// <summary>
    /// The storage repository to use for data access.
    /// </summary>
    public IStorageRepository Repository => repository;

    /// <summary>
    /// The logger for diagnostics and reporting.
    /// </summary>
    public ILogger Logger => logger;
}
