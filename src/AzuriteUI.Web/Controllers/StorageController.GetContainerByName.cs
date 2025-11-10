using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    /// <summary>
    /// Retrieves the properties of a specific container by its name.
    /// </summary>
    /// <remarks>
    /// This endpoint supports RFC 7232 conditional requests using ETag and Last-Modified headers.
    /// </remarks>
    /// <param name="containerName">The name of the container to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The container properties if found; otherwise, a NotFound result.</returns>
    [HttpGet("{containerName}")]
    [EndpointName("GetContainerByName")]
    [EndpointDescription("Retrieves the properties of a specific container by its name.")]
    [ProducesResponseType<ContainerDTO>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public virtual async Task<IActionResult> GetContainerByNameAsync(
        [FromRoute] string containerName,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("GetContainerByNameAsync({containerName})", containerName);

        ContainerDTO? container = await Repository.GetContainerAsync(containerName, cancellationToken);
        if (container is null)
        {
            return NotFound();
        }

        int? statusCode = GetResponseForConditionalRequest(container);
        return statusCode.HasValue ? ConditionalResponse(statusCode.Value, container) : Ok(container);
    }
}