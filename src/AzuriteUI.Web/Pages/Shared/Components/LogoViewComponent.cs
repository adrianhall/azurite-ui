using Microsoft.AspNetCore.Mvc;

namespace AzuriteUI.Web.Pages.Shared.Components;

/// <summary>
/// View component for the application logo.
/// This component is designed to be easily replaceable by rendering
/// a simple template with the logo icon.
/// </summary>
public class LogoViewComponent : ViewComponent
{
    /// <summary>
    /// Invokes the logo view component.
    /// </summary>
    /// <returns>The view component result.</returns>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
