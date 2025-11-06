using AzuriteUI.Web.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.OData.Edm;

namespace AzuriteUI.Web.Controllers;

/// <summary>
/// The controller that manages all the endpoints under <c>/api/uploads</c>, which handles
/// blob upload session management including block uploads, commits, and cancellations.
/// </summary>
/// <param name="repository">The storage repository to use for data access.</param>
/// <param name="odataModel">The <see cref="IEdmModel"/> to use for OData operations.</param>
/// <param name="logger">The logger to use for diagnostics and reporting.</param>
[ApiController]
[Route("api/uploads")]
public partial class UploadsController(
    IStorageRepository repository,
    IEdmModel odataModel,
    ILogger<UploadsController> logger
) : ODataController
{
    /// <summary>
    /// The storage repository to use for data access.
    /// </summary>
    public IStorageRepository Repository => repository;

    /// <summary>
    /// The EDM model for OData.
    /// </summary>
    public IEdmModel EdmModel => odataModel;

    /// <summary>
    /// The logger for diagnostics and reporting.
    /// </summary>
    public ILogger Logger => logger;

    /// <summary>
    /// The default page size for OData queries.
    /// </summary>
    public const int ODataPageSize = 25;

    /// <summary>
    /// The maximum allowed value for $top in OData queries.
    /// </summary>
    public const int ODataMaxTop = 250;
}
