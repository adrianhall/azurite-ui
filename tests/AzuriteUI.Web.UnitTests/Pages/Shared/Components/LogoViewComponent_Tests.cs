using AzuriteUI.Web.Pages.Shared.Components;
using Xunit;

namespace AzuriteUI.Web.UnitTests.Pages.Shared.Components;

/// <summary>
/// Unit tests for the <see cref="LogoViewComponent"/>.
/// </summary>
public class LogoViewComponent_Tests
{
    [Fact]
    public void Invoke_ReturnsViewComponentResult()
    {
        // Arrange
        var component = new LogoViewComponent();

        // Act
        var result = component.Invoke();

        // Assert
        Assert.NotNull(result);
    }
}
