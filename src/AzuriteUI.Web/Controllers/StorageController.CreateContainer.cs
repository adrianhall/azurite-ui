using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;
using System.Text.Json;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    /// <summary>
    /// Creates a new container in the storage account.
    /// </summary>
    /// <param name="dto">The container properties to create.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPost()]
    [EndpointName("CreateContainer")]
    [EndpointDescription("Creates a new container in the storage account.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ContainerDTO>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    public virtual async Task<IActionResult> CreateContainerAsync(
        [FromBody] CreateContainerDTO dto,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("CreateContainerAsync('{containerProps}') called", JsonSerializer.Serialize(dto));

        ContainerDTO createdContainer = await Repository.CreateContainerAsync(dto, cancellationToken);
        return CreatedAtAction("GetContainerByName", new { containerName = createdContainer.Name }, createdContainer);
    }
}