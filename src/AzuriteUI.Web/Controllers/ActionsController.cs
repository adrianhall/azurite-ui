using AzuriteUI.Web.Services.CacheSync;
using Microsoft.AspNetCore.Mvc;

namespace AzuriteUI.Web.Controllers;

/// <summary>
/// The controller that manages the dashboard endpoint at <c>/api/dashboard</c>.
/// </summary>
/// <param name="queueManager">The queue manager to use for managing background tasks.</param>
/// <param name="logger">The logger to use for diagnostics and reporting.</param>
[ApiController]
[Route("api/actions")]
public class ActionsController(
    IQueueManager queueManager,
    ILogger<ActionsController> logger
) : ControllerBase
{
    /// <summary>
    /// Triggers a cache synchronization operation.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The result of the operation</returns>
    [HttpPost("sync-cache")]
    [EndpointName("SyncCache")]
    [EndpointDescription("Triggers a cache synchronization operation.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SyncCacheAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SyncCacheAsync() called");

        var result = await queueManager.EnqueueWorkAsync(cancellationToken);
        return Accepted(result);
    }
}