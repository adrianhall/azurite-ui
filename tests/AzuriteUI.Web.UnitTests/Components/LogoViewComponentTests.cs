using AzuriteUI.Web.Components;
using Xunit;

namespace AzuriteUI.Web.UnitTests.Components;

/// <summary>
/// Unit tests for the <see cref="LogoViewComponent"/>.
/// </summary>
public class LogoViewComponentTests
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
