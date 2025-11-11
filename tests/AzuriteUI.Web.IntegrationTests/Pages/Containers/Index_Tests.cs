using AngleSharp;
using AngleSharp.Dom;
using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Services.Repositories.Models;

namespace AzuriteUI.Web.IntegrationTests.Pages.Containers;

/// <summary>
/// Integration tests for the Containers list page (Containers/Index.cshtml).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Integration test class")]
public class Index_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Helper Methods

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
    public async Task ContainersIndex_ShouldRenderBreadcrumbAndCreateButton()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify breadcrumb
        var breadcrumb = GetElementByTestId(document, "breadcrumb-containers");
        breadcrumb.Should().NotBeNull();
        breadcrumb!.TextContent.Trim().Should().Be("Containers");

        // Verify create container button
        var createButton = GetElementByTestId(document, "create-container-button");
        createButton.Should().NotBeNull()
            .And.HaveAttribute("data-bs-toggle", "modal")
            .And.HaveAttribute("data-bs-target", "#createContainerModal");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldRenderTableWithHeaders()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify table exists
        var table = GetElementByTestId(document, "containers-table");
        table.Should().NotBeNull();

        // Verify all headers are present
        GetElementByTestId(document, "header-name").Should().NotBeNull();
        GetElementByTestId(document, "header-lastmodified").Should().NotBeNull();
        GetElementByTestId(document, "header-count").Should().NotBeNull();
        GetElementByTestId(document, "header-size").Should().NotBeNull();
        GetElementByTestId(document, "header-actions").Should().NotBeNull();

        // Verify initial loading row is present
        var loadingRow = GetElementByTestId(document, "loading-row");
        loadingRow.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldHaveSortableHeaders()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify sortable headers have data-sort attribute
        var nameHeader = GetElementByTestId(document, "header-name");
        nameHeader.Should().NotBeNull()
            .And.HaveAttribute("data-sort", "name");
        nameHeader!.ClassList.Contains("sortable").Should().BeTrue();

        var lastModifiedHeader = GetElementByTestId(document, "header-lastmodified");
        lastModifiedHeader.Should().NotBeNull()
            .And.HaveAttribute("data-sort", "lastModified");
        lastModifiedHeader!.ClassList.Contains("sortable").Should().BeTrue();

        var countHeader = GetElementByTestId(document, "header-count");
        countHeader.Should().NotBeNull()
            .And.HaveAttribute("data-sort", "blobCount");
        countHeader!.ClassList.Contains("sortable").Should().BeTrue();

        var sizeHeader = GetElementByTestId(document, "header-size");
        sizeHeader.Should().NotBeNull()
            .And.HaveAttribute("data-sort", "totalSize");
        sizeHeader!.ClassList.Contains("sortable").Should().BeTrue();

        // Verify sort icons are present
        GetElementByTestId(document, "sort-icon-name").Should().NotBeNull();
        GetElementByTestId(document, "sort-icon-lastmodified").Should().NotBeNull();
        GetElementByTestId(document, "sort-icon-count").Should().NotBeNull();
        GetElementByTestId(document, "sort-icon-size").Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_NameHeader_ShouldShowDescendingSortIcon()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify name is sorted ascending by default (chevron-down icon)
        var sortIcon = GetElementByTestId(document, "sort-icon-name");
        sortIcon.Should().NotBeNull();
        sortIcon!.ClassList.Contains("bi-chevron-down").Should().BeTrue();
    }

    #endregion

    #region Modal Tests

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldIncludeCreateContainerModal()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
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
        containerNameInput.GetAttribute("minlength").Should().Be("3");
        containerNameInput.GetAttribute("maxlength").Should().Be("63");

        // Verify modal buttons
        var cancelButton = GetElementByTestId(document, "modal-cancel");
        cancelButton.Should().NotBeNull().And.HaveTextContent("Cancel");

        var createButton = GetElementByTestId(document, "modal-create");
        createButton.Should().NotBeNull().And.HaveTextContent("Create");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldIncludeDeleteContainerModal()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var modal = GetElementByTestId(document, "delete-container-modal");
        modal.Should().NotBeNull();
        modal!.GetAttribute("id").Should().Be("deleteContainerModal");

        // Verify modal body
        var modalBody = GetElementByTestId(document, "delete-modal-body");
        modalBody.Should().NotBeNull();

        // Verify delete container name placeholder
        var containerName = GetElementByTestId(document, "delete-container-name");
        containerName.Should().NotBeNull();

        // Verify modal buttons
        var cancelButton = GetElementByTestId(document, "delete-modal-cancel");
        cancelButton.Should().NotBeNull().And.HaveTextContent("Cancel");

        var confirmButton = GetElementByTestId(document, "delete-modal-confirm");
        confirmButton.Should().NotBeNull().And.HaveTextContent("Delete");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldIncludeInfoPanel()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var panel = GetElementByTestId(document, "container-info-panel");
        panel.Should().NotBeNull();
        panel!.GetAttribute("id").Should().Be("containerInfoPanel");
        panel.ClassList.Contains("offcanvas").Should().BeTrue();
        panel.ClassList.Contains("offcanvas-end").Should().BeTrue();

        // Verify panel content area
        var content = GetElementByTestId(document, "container-info-content");
        content.Should().NotBeNull();
    }

    #endregion

    #region Load More Tests

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldIncludeLoadMoreButton()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var loadMoreContainer = GetElementByTestId(document, "load-more-container");
        loadMoreContainer.Should().NotBeNull();

        var loadMoreButton = GetElementByTestId(document, "load-more-button");
        loadMoreButton.Should().NotBeNull();
    }

    #endregion

    #region Script Tests

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldIncludeJavaScriptForDynamicLoading()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify key JavaScript functions are present
        html.Should().Contain("loadContainers");
        html.Should().Contain("renderContainers");
        html.Should().Contain("handleSort");
        html.Should().Contain("showDeleteModal");
        html.Should().Contain("showInfoPanel");
        html.Should().Contain("handleCreateContainer");
        html.Should().Contain("handleDeleteContainer");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldIncludeUtilityFunctions()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify utility functions are present
        html.Should().Contain("formatRelativeTime");
        html.Should().Contain("formatFileSize");
        html.Should().Contain("escapeHtml");
        html.Should().Contain("buildApiUrl");
    }

    #endregion

    #region Integration with API Tests

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_WithNoContainers_ApiShouldReturnEmptyList()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act - Call the API that the page will use
        var apiResponse = await client.GetAsync("/api/containers?$top=25&$orderby=name");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<ContainerDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().BeEmpty();
        data.TotalCount.Should().Be(0);
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_WithContainers_ApiShouldReturnContainersList()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("container1");
        await Fixture.Azurite.CreateContainerAsync("container2");
        await Fixture.Azurite.CreateContainerAsync("container3");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act - Call the API that the page will use
        var apiResponse = await client.GetAsync("/api/containers?$top=25&$orderby=name");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<ContainerDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().HaveCount(3);
        data.TotalCount.Should().Be(3);

        // Verify containers are sorted by name
        var containerNames = data.Items.Select(c => c.Name).ToList();
        containerNames.Should().BeInAscendingOrder();
        containerNames.Should().Contain("container1");
        containerNames.Should().Contain("container2");
        containerNames.Should().Contain("container3");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ApiWithDescendingSort_ShouldReturnSortedContainers()
    {
        // Arrange
        await Fixture.Azurite.CreateContainerAsync("aaa-container");
        await Fixture.Azurite.CreateContainerAsync("bbb-container");
        await Fixture.Azurite.CreateContainerAsync("ccc-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act - Call the API with descending sort
        var apiResponse = await client.GetAsync("/api/containers?$top=25&$orderby=name desc");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<ContainerDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().HaveCount(3);

        // Verify containers are sorted descending by name
        var containerNames = data.Items.Select(c => c.Name).ToList();
        containerNames.Should().BeInDescendingOrder();
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ApiWithPagination_ShouldReturnNextLink()
    {
        // Arrange - Create 30 containers (more than default page size of 25)
        for (int i = 1; i <= 30; i++)
        {
            await Fixture.Azurite.CreateContainerAsync($"container-{i:D3}");
        }
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act - Get first page
        var apiResponse = await client.GetAsync("/api/containers?$top=25&$orderby=name");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<ContainerDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().HaveCount(25);
        data.TotalCount.Should().Be(30);
        data.FilteredCount.Should().Be(30);
        data.NextLink.Should().NotBeNullOrEmpty();
        data.NextLink.Should().Contain("$skip=25");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ApiWithContainersWithBlobs_ShouldReturnBlobCountAndSize()
    {
        // Arrange
        var container = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(container, "blob1.txt", "Content 1");
        await Fixture.Azurite.CreateBlobAsync(container, "blob2.txt", "Content 2");
        await Fixture.Azurite.CreateBlobAsync(container, "blob3.txt", "Content 3");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var apiResponse = await client.GetAsync("/api/containers?$top=25&$orderby=name");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<ContainerDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().HaveCount(1);

        var containerDto = data.Items.First();
        containerDto.Name.Should().Be(container);
        containerDto.BlobCount.Should().Be(3);
        containerDto.TotalSize.Should().BeGreaterThan(0);
        containerDto.ETag.Should().NotBeNullOrEmpty();
        containerDto.LastModified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Page Title Tests

    [Fact(Timeout = 60000)]
    public async Task ContainersIndex_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var title = document.QuerySelector("title");
        title.Should().NotBeNull();
        title!.TextContent.Should().Contain("Containers");
    }

    #endregion
}
