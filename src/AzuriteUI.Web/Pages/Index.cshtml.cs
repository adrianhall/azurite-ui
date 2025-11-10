using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AzuriteUI.Web.Pages;

/// <summary>
/// Page model for the dashboard page.
/// </summary>
/// <param name="repository">The storage repository for accessing data.</param>
/// <param name="logger">The logger for diagnostics.</param>
public class IndexModel(IStorageRepository repository, ILogger<IndexModel> logger) : PageModel
{
    /// <summary>
    /// Gets the dashboard data.
    /// </summary>
    public DashboardResponse? Dashboard { get; private set; }

    /// <summary>
    /// Handles GET requests to the dashboard page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Dashboard = await repository.GetDashboardDataAsync(HttpContext.RequestAborted);
            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load dashboard data");
            // Set empty dashboard data so the page still renders
            Dashboard = new DashboardResponse
            {
                Stats = new DashboardStats
                {
                    Containers = 0,
                    Blobs = 0,
                    TotalBlobSize = 0,
                    TotalImageSize = 0
                },
                RecentContainers = [],
                RecentBlobs = []
            };
            // TODO: Display error message as toast.
            return Page();
        }
    }
}
