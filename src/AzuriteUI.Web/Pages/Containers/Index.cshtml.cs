using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AzuriteUI.Web.Pages.Containers;

/// <summary>
/// Page model for the containers list page.
/// </summary>
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostics.</param>
    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles GET requests to the containers list page.
    /// </summary>
    public void OnGet()
    {
        _logger.LogInformation("Containers list page accessed");
    }
}
