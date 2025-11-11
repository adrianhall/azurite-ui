using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Pages;
using AzuriteUI.Web.Services.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AzuriteUI.Web.UnitTests.Pages;

/// <summary>
/// Unit tests for the <see cref="IndexModel"/>.
/// </summary>
public class IndexModel_Tests
{
    [Fact]
    public async Task OnGetAsync_WhenSuccessful_ReturnsDashboardData()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var logger = Substitute.For<ILogger<IndexModel>>();
        var expectedDashboard = new DashboardResponse
        {
            Stats = new DashboardStats
            {
                Containers = 5,
                Blobs = 10,
                TotalBlobSize = 1024,
                TotalImageSize = 512
            },
            RecentContainers = [],
            RecentBlobs = []
        };

        repository.GetDashboardDataAsync(Arg.Any<CancellationToken>())
            .Returns(expectedDashboard);

        var pageModel = new IndexModel(repository, logger);
        pageModel.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<PageResult>(result);
        Assert.NotNull(pageModel.Dashboard);
        Assert.Equal(expectedDashboard, pageModel.Dashboard);
    }

    [Fact]
    public async Task OnGetAsync_WhenExceptionThrown_SetsEmptyDashboardAndLogsError()
    {
        // Arrange
        var repository = Substitute.For<IStorageRepository>();
        var logger = Substitute.For<ILogger<IndexModel>>();
        var expectedException = new InvalidOperationException("Test exception");

        repository.GetDashboardDataAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        var pageModel = new IndexModel(repository, logger);
        pageModel.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<PageResult>(result);
        Assert.NotNull(pageModel.Dashboard);

        // Verify the dashboard is set with empty data
        Assert.NotNull(pageModel.Dashboard.Stats);
        Assert.Equal(0, pageModel.Dashboard.Stats.Containers);
        Assert.Equal(0, pageModel.Dashboard.Stats.Blobs);
        Assert.Equal(0, pageModel.Dashboard.Stats.TotalBlobSize);
        Assert.Equal(0, pageModel.Dashboard.Stats.TotalImageSize);
        Assert.Empty(pageModel.Dashboard.RecentContainers);
        Assert.Empty(pageModel.Dashboard.RecentBlobs);

        // Verify the error was logged
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to load dashboard data")),
            expectedException,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
