using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Net.Mime;
using System.Text.Json;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    /// <summary>
    /// Updates an existing container with new metadata.
    /// </summary>
    /// <param name="containerName">The name of the container to create or update.</param>
    /// <param name="dto">The container properties to set.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An <see cref="OkObjectResult"/> or <see cref="CreatedResult"/> response object with the container.</returns>
    [HttpPut("{containerName}")]
    [EndpointName("CreateOrUpdateContainer")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<ContainerDTO>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]  
    public virtual async Task<IActionResult> UpdateContainerAsync(
        [FromRoute] string containerName,
        [FromBody] UpdateContainerDTO dto,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("UpdateContainerAsync('{containerName}', '{containerProps}') called", containerName, JsonSerializer.Serialize(dto));

        dto.ContainerName = dto.ContainerName.OrDefault(containerName);
        if (dto.ContainerName != containerName)
        {
            Logger.LogWarning("UpdateContainerAsync: Mismatch between route containerName '{RouteContainerName}' and body containerName '{BodyContainerName}'", containerName, dto.ContainerName);
            return BadRequest("Container name in the URL must match the container name in the request body.");
        }

        ContainerDTO? existingContainer = await Repository.GetContainerAsync(containerName, cancellationToken);
        if (existingContainer is null)
        {
            Logger.LogWarning("UpdateContainerAsync: Container '{containerName}' not found.", containerName);
            return NotFound();
        }

        int? statusCode = GetResponseForConditionalRequest(existingContainer);
        if (statusCode.HasValue)
        {
            Logger.LogInformation("UpdateContainerAsync: Conditional request for container '{containerName}' resulted in {StatusCode}", containerName, statusCode.Value);
            return ConditionalResponse(statusCode.Value, existingContainer);
        }

        ContainerDTO updatedContainer = await Repository.UpdateContainerAsync(dto, cancellationToken);
        return Ok(updatedContainer);
    }  
}