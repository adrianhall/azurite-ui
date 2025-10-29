using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.Azurite.Models;
using AzuriteUI.Web.Services.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace AzuriteUI.Web.UnitTests.Services.Health;

[ExcludeFromCodeCoverage(Justification = "Test class")]
public class AzuriteHealthCheck_Tests
{
    private readonly IAzuriteService _mockAzuriteService;
    private readonly AzuriteHealthCheck _healthCheck;
    private readonly HealthCheckContext _healthCheckContext;

    public AzuriteHealthCheck_Tests()
    {
        _mockAzuriteService = Substitute.For<IAzuriteService>();
        _healthCheck = new AzuriteHealthCheck(_mockAzuriteService);
        _healthCheckContext = new HealthCheckContext();
    }

    #region CheckHealthAsync

    [Fact]
    public async Task CheckHealthAsync_WhenServiceIsHealthy_ReturnsHealthyResult()
    {
        // Arrange
        var expectedConnectionString = "UseDevelopmentStorage=true";
        var expectedResponseTime = 150L;
        var healthStatus = new AzuriteHealthStatus
        {
            IsHealthy = true,
            ConnectionString = expectedConnectionString,
            ResponseTimeMilliseconds = expectedResponseTime
        };
        _mockAzuriteService.GetHealthStatusAsync(Arg.Any<CancellationToken>()).Returns(healthStatus);

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("ConnectionString");
        result.Data["ConnectionString"].Should().Be(expectedConnectionString);
        result.Data.Should().ContainKey("ResponseTime");
        result.Data["ResponseTime"].Should().Be(expectedResponseTime);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServiceIsHealthyWithNullResponseTime_ReturnsHealthyResultWithZeroResponseTime()
    {
        // Arrange
        var healthStatus = new AzuriteHealthStatus
        {
            IsHealthy = true,
            ConnectionString = "UseDevelopmentStorage=true",
            ResponseTimeMilliseconds = null
        };
        _mockAzuriteService.GetHealthStatusAsync(Arg.Any<CancellationToken>()).Returns(healthStatus);

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("ResponseTime");
        result.Data["ResponseTime"].Should().Be(0L);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServiceIsUnhealthy_ReturnsUnhealthyResult()
    {
        // Arrange
        var expectedConnectionString = "UseDevelopmentStorage=true";
        var expectedErrorMessage = "Connection failed";
        var healthStatus = new AzuriteHealthStatus
        {
            IsHealthy = false,
            ConnectionString = expectedConnectionString,
            ErrorMessage = expectedErrorMessage
        };
        _mockAzuriteService.GetHealthStatusAsync(Arg.Any<CancellationToken>()).Returns(healthStatus);

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data.Should().ContainKey("ConnectionString");
        result.Data["ConnectionString"].Should().Be(expectedConnectionString);
        result.Data.Should().ContainKey("ErrorMessage");
        result.Data["ErrorMessage"].Should().Be(expectedErrorMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServiceIsUnhealthyWithNullErrorMessage_ReturnsUnhealthyResultWithDefaultError()
    {
        // Arrange
        var healthStatus = new AzuriteHealthStatus
        {
            IsHealthy = false,
            ConnectionString = "UseDevelopmentStorage=true",
            ErrorMessage = null
        };
        _mockAzuriteService.GetHealthStatusAsync(Arg.Any<CancellationToken>()).Returns(healthStatus);

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data.Should().ContainKey("ErrorMessage");
        result.Data["ErrorMessage"].Should().Be("Unknown error");
    }

    [Fact]
    public async Task CheckHealthAsync_PassesCancellationTokenToService()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var healthStatus = new AzuriteHealthStatus
        {
            IsHealthy = true,
            ConnectionString = "UseDevelopmentStorage=true",
            ResponseTimeMilliseconds = 100L
        };
        _mockAzuriteService.GetHealthStatusAsync(Arg.Any<CancellationToken>()).Returns(healthStatus);

        // Act
        await _healthCheck.CheckHealthAsync(_healthCheckContext, cancellationToken);

        // Assert
        await _mockAzuriteService.Received(1).GetHealthStatusAsync(cancellationToken);
    }

    #endregion
}