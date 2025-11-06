using AzuriteUI.Web.Services.Azurite.Exceptions;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AzuriteUI.Web.Controllers;

public partial class StorageController : ODataController
{
    /// <summary>
    /// Deletes a specific container by its name, including all blobs within the container.
    /// </summary>
    /// <param name="containerName">The name of the container to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpDelete("{containerName}")]
    [EndpointName("DeleteContainer")]
    [EndpointDescription("Deletes a specific container by its name.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public virtual async Task<IActionResult> DeleteContainerAsync(
        [FromRoute] string containerName,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("DeleteContainerAsync({containerName})", containerName);

        ContainerDTO? container = await Repository.GetContainerAsync(containerName, cancellationToken);
        if (container is null)
        {
            Logger.LogInformation("DeleteContainerAsync: Container '{containerName}' not found", containerName);
            return NotFound();
        }

        int? statusCode = GetResponseForConditionalRequest(container);
        if (statusCode.HasValue)
        {
            Logger.LogInformation("DeleteContainerAsync: Conditional request for container '{containerName}' resulted in {StatusCode}", containerName, statusCode.Value);
            return ConditionalResponse(statusCode.Value, container);
        }

        try
        {
            Logger.LogInformation("DeleteContainerAsync: Deleting container '{containerName}'", containerName);
            await Repository.DeleteContainerAsync(containerName, cancellationToken);
            return NoContent();
        }
        catch (AzuriteServiceException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            Logger.LogInformation("DeleteContainerAsync: Container '{containerName}' not found in Azurite during deletion", containerName);
            return NoContent();
        }
    }
}