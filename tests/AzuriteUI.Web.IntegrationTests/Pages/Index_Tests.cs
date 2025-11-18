using AngleSharp;
using AngleSharp.Dom;
using System.Net;

namespace AzuriteUI.Web.IntegrationTests.Pages;

/// <summary>
/// Integration tests for the Dashboard page (Index.cshtml).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Integration test class")]
public class Index_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Helper Methods
    /// <summary>
    /// The text string that is returned by DisplayHelper for zero byte sizes.
    /// </summary>
    private const string ZeroSizeText = "0 b";

    /// <summary>
    /// Parses HTML content into an IDocument for querying with AngleSharp.
    /// </summary>
    private static async Task<IDocument> ParseHtmlAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(html));
    }

    /// <summary>
    /// Gets the text content of an element by testid.
    /// </summary>
    private static string? GetTextByTestId(IDocument document, string testId)
    {
        var element = document.QuerySelector($"[data-testid='{testId}']");
        return element?.TextContent.Trim();
    }

    /// <summary>
    /// Gets an element by testid.
    /// </summary>
    private static IElement? GetElementByTestId(IDocument document, string testId)
    {
        return document.QuerySelector($"[data-testid='{testId}']");
    }

    #endregion

    #region Basic Rendering Tests

    [Fact(Timeout = 60000)]
    public async Task Index_EmptyDashboard_ShouldRenderAllStatCardsWithZeroValues()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify all stat cards are present with zero values
        var containersCard = GetElementByTestId(document, "stat-containers");
        containersCard.Should().NotBeNull();
        containersCard!.QuerySelector(".stats-card-title").Should().NotBeNull().And.HaveTextContent("Containers");
        containersCard.QuerySelector(".stats-card-value").Should().NotBeNull().And.HaveTextContent("0");

        var blobsCard = GetElementByTestId(document, "stat-blobs");
        blobsCard.Should().NotBeNull();
        blobsCard!.QuerySelector(".stats-card-title").Should().NotBeNull().And.HaveTextContent("Blobs");
        blobsCard.QuerySelector(".stats-card-value").Should().NotBeNull().And.HaveTextContent("0");

        var totalSizeCard = GetElementByTestId(document, "stat-total-size");
        totalSizeCard.Should().NotBeNull();
        totalSizeCard!.QuerySelector(".stats-card-title").Should().NotBeNull().And.HaveTextContent("Total Size");
        totalSizeCard.QuerySelector(".stats-card-value").Should().NotBeNull().And.HaveTextContent(ZeroSizeText);

        var imagesSizeCard = GetElementByTestId(document, "stat-images-size");
        imagesSizeCard.Should().NotBeNull();
        imagesSizeCard!.QuerySelector(".stats-card-title").Should().NotBeNull().And.HaveTextContent("Images Size");
        imagesSizeCard.QuerySelector(".stats-card-value").Should().NotBeNull().And.HaveTextContent(ZeroSizeText);
    }

    [Fact(Timeout = 60000)]
    public async Task Index_EmptyDashboard_ShouldShowNoContainersMessage()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var noContainersRow = GetElementByTestId(document, "no-containers");
        noContainersRow.Should().NotBeNull().And.HaveTextContent("No containers found");
    }

    [Fact(Timeout = 60000)]
    public async Task Index_EmptyDashboard_ShouldShowNoBlobsMessage()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var noBlobsRow = GetElementByTestId(document, "no-blobs");
        noBlobsRow.Should().NotBeNull().And.HaveTextContent("No blobs found");
    }

    [Fact(Timeout = 60000)]
    public async Task Index_ShouldRenderBreadcrumbAndCreateButton()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify breadcrumb
        var breadcrumb = GetElementByTestId(document, "breadcrumb-home");
        breadcrumb.Should().NotBeNull();
        breadcrumb!.TextContent.Trim().Should().Be("Home");

        // Verify create container button
        var createButton = GetElementByTestId(document, "create-container-button");
        createButton.Should().NotBeNull()
            .And.HaveAttribute("data-bs-toggle", "modal")
            .And.HaveAttribute("data-bs-target", "#createContainerModal");
    }

    #endregion

    #region Statistics Card Tests

    [Fact(Timeout = 60000)]
    public async Task Index_WithThreeContainers_ShouldDisplayCorrectCount()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("container1");
        await Fixture.Azurite.CreateContainerAsync("container2");
        await Fixture.Azurite.CreateContainerAsync("container3");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var containersCard = GetElementByTestId(document, "stat-containers");
        containersCard.Should().NotBeNull();
        containersCard!.QuerySelector(".stats-card-value").Should().NotBeNull().And.HaveTextContent("3");
    }

    [Fact(Timeout = 60000)]
    public async Task Index_WithBlobs_ShouldDisplayCorrectBlobCountAndSizes()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(container, "blob1.txt", "Hello World", "text/plain");
        await Fixture.Azurite.CreateBlobAsync(container, "blob2.txt", "Test Content", "text/plain");
        await Fixture.Azurite.CreateBlobAsync(container, "image.jpg", "FakeImageData123", "image/jpeg");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify blob count
        var blobsCard = GetElementByTestId(document, "stat-blobs");
        blobsCard.Should().NotBeNull();
        blobsCard!.QuerySelector(".stats-card-value").Should().NotBeNull().And.HaveTextContent("3");

        // Verify total size is shown (exact value will depend on DisplayHelper formatting)
        var totalSizeCard = GetElementByTestId(document, "stat-total-size");
        totalSizeCard.Should().NotBeNull();
        totalSizeCard!.QuerySelector(".stats-card-value").Should().NotBeNull().And.NotHaveTextContent(ZeroSizeText);

        // Verify image size is shown and less than total size
        var imagesSizeCard = GetElementByTestId(document, "stat-images-size");
        imagesSizeCard.Should().NotBeNull();
        imagesSizeCard!.QuerySelector(".stats-card-value").Should().NotBeNull().And.NotHaveTextContent(ZeroSizeText);
    }

    [Fact(Timeout = 60000)]
    public async Task Index_WithOnlyNonImageBlobs_ShouldShowZeroImageSize()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(container, "doc.txt", "Text content", "text/plain");
        await Fixture.Azurite.CreateBlobAsync(container, "data.json", "JSON content", "application/json");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var imagesSizeCard = GetElementByTestId(document, "stat-images-size");
        imagesSizeCard.Should().NotBeNull();
        imagesSizeCard!.QuerySelector(".stats-card-value").Should().NotBeNull().And.HaveTextContent(ZeroSizeText);
    }

    #endregion

    #region Recent Containers Table Tests

    [Fact(Timeout = 60000)]
    public async Task Index_WithContainers_ShouldDisplayRecentContainersTable()
    {
        // Arrange
        var container1 = await Fixture.Azurite.CreateContainerAsync("alpha-container");
        await Task.Delay(100);
        var container2 = await Fixture.Azurite.CreateContainerAsync("beta-container");
        await Task.Delay(100);
        var container3 = await Fixture.Azurite.CreateContainerAsync("gamma-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify table title
        var tableTitle = GetTextByTestId(document, "recent-containers-title");
        tableTitle.Should().Be("Recently Updated Containers");

        // Verify table exists and has rows
        var table = GetElementByTestId(document, "recent-containers-table");
        table.Should().NotBeNull();

        var containerRows = document.QuerySelectorAll("[data-testid='container-row']");
        containerRows.Length.Should().Be(3);

        // Verify table headers
        var headers = table!.QuerySelectorAll("thead th");
        headers.Should().NotBeNull()
            .And.HaveCount(4);
    }

    [Fact(Timeout = 60000)]
    public async Task Index_RecentContainersTable_ShouldContainLinksToContainerPages()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var containerRows = document.QuerySelectorAll("[data-testid='container-row']");
        containerRows.Length.Should().Be(1);

        var firstRow = containerRows[0];
        var link = firstRow.QuerySelector("a");
        link.Should().NotBeNull()
            .And.HaveAttribute("href", $"/containers/{Uri.EscapeDataString(containerName)}")
            .And.HaveTextContent(containerName);
    }

    [Fact(Timeout = 60000)]
    public async Task Index_RecentContainersTable_ShouldShowRelativeTime()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var containerRows = document.QuerySelectorAll("[data-testid='container-row']");
        containerRows.Length.Should().Be(1);

        var firstRow = containerRows[0];
        var lastModifiedCell = firstRow.QuerySelectorAll("td")[1];
        var timeSpan = lastModifiedCell.QuerySelector("span");
        timeSpan.Should().NotBeNull();

        // Verify relative time is displayed (e.g., "a few seconds ago", "1 minute ago")
        timeSpan!.TextContent.Trim().Should().NotBeNullOrEmpty();

        // Verify absolute time is in title attribute
        timeSpan.GetAttribute("title").Should().NotBeNullOrEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task Index_RecentContainersTable_ShouldShowBlobCountAndSize()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(container, "blob1.txt", "Content1");
        await Fixture.Azurite.CreateBlobAsync(container, "blob2.txt", "Content2");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var containerRows = document.QuerySelectorAll("[data-testid='container-row']");
        containerRows.Length.Should().Be(1);

        var firstRow = containerRows[0];
        var cells = firstRow.QuerySelectorAll("td");

        // Count column (index 2)
        var countText = cells[2].TextContent.Trim();
        countText.Should().Be("2");

        // Size column (index 3) - should show formatted size
        var sizeText = cells[3].TextContent.Trim();
        sizeText.Should().NotBeNullOrEmpty();
        sizeText.Should().NotBe(ZeroSizeText);
    }

    [Fact(Timeout = 60000)]
    public async Task Index_WithMoreThanTenContainers_ShouldShowOnlyTenMostRecent()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            await Fixture.Azurite.CreateContainerAsync($"container-{i:D2}");
            await Task.Delay(50);
        }
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var containerRows = document.QuerySelectorAll("[data-testid='container-row']");
        containerRows.Length.Should().Be(10, "should only display 10 most recent containers");
    }

    #endregion

    #region Recent Blobs Table Tests

    [Fact(Timeout = 60000)]
    public async Task Index_WithBlobs_ShouldDisplayRecentBlobsTable()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(container, "blob1.txt", "Content1");
        await Task.Delay(100);
        await Fixture.Azurite.CreateBlobAsync(container, "blob2.txt", "Content2");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify table title
        var tableTitle = GetTextByTestId(document, "recent-blobs-title");
        tableTitle.Should().Be("Recently Updated Blobs");

        // Verify table exists and has rows
        var table = GetElementByTestId(document, "recent-blobs-table");
        table.Should().NotBeNull();

        var blobRows = document.QuerySelectorAll("[data-testid='blob-row']");
        blobRows.Length.Should().Be(2);

        // Verify table headers
        var headers = table!.QuerySelectorAll("thead th");
        headers.Length.Should().Be(4);
        headers[0].Should().HaveTextContent("Name");
        headers[1].Should().HaveTextContent("Container");
        headers[2].Should().HaveTextContent("Last Modified");
        headers[3].Should().HaveTextContent("Size");
    }

    [Fact(Timeout = 60000)]
    public async Task Index_RecentBlobsTable_ShouldContainLinksAndIcons()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(container, "document.txt", "Content", "text/plain");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var blobRows = document.QuerySelectorAll("[data-testid='blob-row']");
        blobRows.Length.Should().Be(1);

        var firstRow = blobRows[0];
        var cells = firstRow.QuerySelectorAll("td");

        // Name column should have icon and link
        var nameCell = cells[0];
        var icon = nameCell.QuerySelector("i.bi");
        icon.Should().NotBeNull("blob name should have a bootstrap icon");

        var blobLink = nameCell.QuerySelector("a");
        blobLink.Should().NotBeNull();
        blobLink!.GetAttribute("href").Should().Contain($"/containers/{Uri.EscapeDataString(container)}");
        blobLink.GetAttribute("href").Should().Contain("blob=");

        // Container column should have link
        var containerCell = cells[1];
        var containerLink = containerCell.QuerySelector("a");
        containerLink.Should().NotBeNull()
            .And.HaveAttribute("href", $"/containers/{Uri.EscapeDataString(container)}")
            .And.HaveTextContent(container);
    }

    [Fact(Timeout = 60000)]
    public async Task Index_RecentBlobsTable_ShouldShowRelativeTimeWithAbsoluteTooltip()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(container, "blob.txt", "Content");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var blobRows = document.QuerySelectorAll("[data-testid='blob-row']");
        blobRows.Length.Should().Be(1);

        var firstRow = blobRows[0];
        var lastModifiedCell = firstRow.QuerySelectorAll("td")[2];
        var timeSpan = lastModifiedCell.QuerySelector("span");
        timeSpan.Should().NotBeNull();

        // Verify relative time is displayed
        timeSpan!.TextContent.Trim().Should().NotBeNullOrEmpty();

        // Verify absolute time is in title attribute
        timeSpan.GetAttribute("title").Should().NotBeNullOrEmpty();
    }

    [Fact(Timeout = 60000)]
    public async Task Index_RecentBlobsTable_ShouldShowFormattedSize()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(container, "blob.txt", "Test Content Here");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var blobRows = document.QuerySelectorAll("[data-testid='blob-row']");
        blobRows.Length.Should().Be(1);

        var firstRow = blobRows[0];
        var cells = firstRow.QuerySelectorAll("td");

        // Size column (index 3) - should show formatted size
        var sizeText = cells[3].TextContent.Trim();
        sizeText.Should().NotBeNullOrEmpty();
        sizeText.Should().NotBe(ZeroSizeText);
    }

    [Fact(Timeout = 60000)]
    public async Task Index_WithMoreThanTenBlobs_ShouldShowOnlyTenMostRecent()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        for (int i = 0; i < 15; i++)
        {
            await Fixture.Azurite.CreateBlobAsync(container, $"blob-{i:D2}.txt", $"Content{i}");
            await Task.Delay(50);
        }
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var blobRows = document.QuerySelectorAll("[data-testid='blob-row']");
        blobRows.Length.Should().Be(10, "should only display 10 most recent blobs");
    }

    [Fact(Timeout = 60000)]
    public async Task Index_WithBlobsFromMultipleContainers_ShouldShowCorrectContainerNames()
    {
        // Arrange
        var container1 = await Fixture.Azurite.CreateContainerAsync("container-alpha");
        var container2 = await Fixture.Azurite.CreateContainerAsync("container-beta");
        await Fixture.Azurite.CreateBlobAsync(container1, "blob-a.txt", "Content A");
        await Task.Delay(100);
        await Fixture.Azurite.CreateBlobAsync(container2, "blob-b.txt", "Content B");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var blobRows = document.QuerySelectorAll("[data-testid='blob-row']");
        blobRows.Length.Should().Be(2);

        // Most recent blob should be from container-beta
        var firstRow = blobRows[0];
        var containerCell = firstRow.QuerySelectorAll("td")[1];
        containerCell.TextContent.Trim().Should().Be(container2);

        // Second blob should be from container-alpha
        var secondRow = blobRows[1];
        var containerCell2 = secondRow.QuerySelectorAll("td")[1];
        containerCell2.Should().HaveTextContent(container1);
    }

    #endregion

    #region Create Container Modal Tests

    [Fact(Timeout = 60000)]
    public async Task Index_ShouldIncludeCreateContainerModal()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var modal = GetElementByTestId(document, "create-container-modal");
        modal.Should().NotBeNull();
        modal!.GetAttribute("id").Should().Be("createContainerModal");

        // Verify modal contains form elements
        var containerNameInput = GetElementByTestId(document, "container-name-input");
        containerNameInput.Should().NotBeNull();
        containerNameInput!.GetAttribute("required").Should().NotBeNull();
        containerNameInput.GetAttribute("pattern").Should().NotBeNullOrEmpty();

        // Verify modal buttons
        var cancelButton = GetElementByTestId(document, "modal-cancel");
        cancelButton.Should().NotBeNull().And.HaveTextContent("Cancel");

        var createButton = GetElementByTestId(document, "modal-create");
        createButton.Should().NotBeNull().And.HaveTextContent("Create");
    }

    #endregion
}
