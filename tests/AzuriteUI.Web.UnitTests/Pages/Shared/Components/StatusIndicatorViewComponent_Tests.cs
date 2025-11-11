using AzuriteUI.Web.Pages.Shared.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Xunit;

namespace AzuriteUI.Web.UnitTests.Pages.Shared.Components;

/// <summary>
/// Unit tests for the <see cref="StatusIndicatorViewComponent"/>.
/// </summary>
public class StatusIndicatorViewComponent_Tests
{
    private readonly HealthCheckService _healthCheckService;
    private readonly StatusIndicatorViewComponent _component;

    public StatusIndicatorViewComponent_Tests()
    {
        _healthCheckService = Substitute.For<HealthCheckService>();
        _component = new StatusIndicatorViewComponent(_healthCheckService);

        // Set up ViewComponentContext for HttpContext
        var httpContext = new DefaultHttpContext();
        var viewContext = new ViewContext { HttpContext = httpContext };
        _component.ViewComponentContext = new ViewComponentContext
        {
            ViewContext = viewContext
        };
    }

    [Fact]
    public async Task InvokeAsync_WhenHealthy_ReturnsConnectedStatus()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.Zero);

        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>>(), Arg.Any<CancellationToken>())
            .Returns(healthReport);

        // Act
        var result = await _component.InvokeAsync();

        // Assert
        Assert.NotNull(result);
        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<StatusIndicatorViewModel>(viewResult.ViewData!.Model);

        Assert.True(model.IsHealthy);
        Assert.Equal("Connected", model.StatusText);
        Assert.Equal("wifi", model.Icon);
        Assert.Equal("connected", model.CssClass);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhealthy_ReturnsDisconnectedStatus()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Unhealthy,
            TimeSpan.Zero);

        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>>(), Arg.Any<CancellationToken>())
            .Returns(healthReport);

        // Act
        var result = await _component.InvokeAsync();

        // Assert
        Assert.NotNull(result);
        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<StatusIndicatorViewModel>(viewResult.ViewData!.Model);

        Assert.False(model.IsHealthy);
        Assert.Equal("Disconnected", model.StatusText);
        Assert.Equal("wifi-off", model.Icon);
        Assert.Equal("disconnected", model.CssClass);
    }

    [Fact]
    public async Task InvokeAsync_WhenDegraded_ReturnsDisconnectedStatus()
    {
        // Arrange
        var healthReport = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Degraded,
            TimeSpan.Zero);

        _healthCheckService
            .CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>>(), Arg.Any<CancellationToken>())
            .Returns(healthReport);

        // Act
        var result = await _component.InvokeAsync();

        // Assert
        Assert.NotNull(result);
        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<StatusIndicatorViewModel>(viewResult.ViewData!.Model);

        Assert.False(model.IsHealthy);
        Assert.Equal("Disconnected", model.StatusText);
        Assert.Equal("wifi-off", model.Icon);
        Assert.Equal("disconnected", model.CssClass);
    }
}
