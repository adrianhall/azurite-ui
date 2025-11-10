using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AzuriteUI.Web.Pages.Containers;

/// <summary>
/// Page model for viewing blobs in a specific container.
/// </summary>
public class ContainerModel : PageModel
{
    private readonly ILogger<ContainerModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerModel"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostics.</param>
    public ContainerModel(ILogger<ContainerModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the container name from the route.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the blob name from the query string (optional, for highlighting a specific blob).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Blob { get; set; }

    /// <summary>
    /// Handles GET requests to the container blobs page.
    /// </summary>
    public void OnGet()
    {
        _logger.LogInformation("Container blobs page accessed for container: {ContainerName}", ContainerName);
    }
}
