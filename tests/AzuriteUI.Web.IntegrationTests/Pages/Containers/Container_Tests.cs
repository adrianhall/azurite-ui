using AngleSharp;
using AngleSharp.Dom;
using System.Net;
using System.Net.Http.Json;
using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Services.Repositories.Models;

namespace AzuriteUI.Web.IntegrationTests.Pages.Containers;

/// <summary>
/// Integration tests for the Blobs page (Containers/Container.cshtml).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Integration test class")]
public class Container_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
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
    public async Task ContainerPage_ShouldRenderBreadcrumbAndUploadButton()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify breadcrumb
        var breadcrumbContainers = GetElementByTestId(document, "breadcrumb-containers");
        breadcrumbContainers.Should().NotBeNull();
        breadcrumbContainers!.TextContent.Trim().Should().Be("Containers");

        var breadcrumbContainer = GetElementByTestId(document, "breadcrumb-container");
        breadcrumbContainer.Should().NotBeNull();
        breadcrumbContainer!.TextContent.Trim().Should().Be(containerName);

        // Verify upload button
        var uploadButton = GetElementByTestId(document, "upload-button");
        uploadButton.Should().NotBeNull()
            .And.HaveAttribute("data-bs-toggle", "modal")
            .And.HaveAttribute("data-bs-target", "#uploadModal");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ShouldRenderTableWithHeaders()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify table exists
        var table = GetElementByTestId(document, "blobs-table");
        table.Should().NotBeNull();

        // Verify all headers are present
        GetElementByTestId(document, "header-name").Should().NotBeNull();
        GetElementByTestId(document, "header-lastmodified").Should().NotBeNull();
        GetElementByTestId(document, "header-type").Should().NotBeNull();
        GetElementByTestId(document, "header-size").Should().NotBeNull();
        GetElementByTestId(document, "header-actions").Should().NotBeNull();

        // Verify initial loading row is present
        var loadingRow = GetElementByTestId(document, "loading-row");
        loadingRow.Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ShouldHaveSortableHeaders()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
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

        var typeHeader = GetElementByTestId(document, "header-type");
        typeHeader.Should().NotBeNull()
            .And.HaveAttribute("data-sort", "contentType");
        typeHeader!.ClassList.Contains("sortable").Should().BeTrue();

        var sizeHeader = GetElementByTestId(document, "header-size");
        sizeHeader.Should().NotBeNull()
            .And.HaveAttribute("data-sort", "contentLength");
        sizeHeader!.ClassList.Contains("sortable").Should().BeTrue();

        // Verify sort icons are present
        GetElementByTestId(document, "sort-icon-name").Should().NotBeNull();
        GetElementByTestId(document, "sort-icon-lastmodified").Should().NotBeNull();
        GetElementByTestId(document, "sort-icon-type").Should().NotBeNull();
        GetElementByTestId(document, "sort-icon-size").Should().NotBeNull();
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_NameHeader_ShouldShowDescendingSortIcon()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
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
    public async Task ContainerPage_ShouldIncludeUploadModal()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var modal = GetElementByTestId(document, "upload-modal");
        modal.Should().NotBeNull();
        modal!.GetAttribute("id").Should().Be("uploadModal");

        // Verify modal contains form elements
        var fileInput = GetElementByTestId(document, "blob-file-input");
        fileInput.Should().NotBeNull();
        fileInput!.GetAttribute("required").Should().NotBeNull();
        fileInput.GetAttribute("type").Should().Be("file");

        // Verify modal buttons
        var cancelButton = GetElementByTestId(document, "modal-cancel");
        cancelButton.Should().NotBeNull().And.HaveTextContent("Cancel");

        var uploadButton = GetElementByTestId(document, "modal-upload");
        uploadButton.Should().NotBeNull().And.HaveTextContent("Upload");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ShouldIncludeDeleteBlobModal()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var modal = GetElementByTestId(document, "delete-blob-modal");
        modal.Should().NotBeNull();
        modal!.GetAttribute("id").Should().Be("deleteBlobModal");

        // Verify modal body
        var modalBody = GetElementByTestId(document, "delete-modal-body");
        modalBody.Should().NotBeNull();

        // Verify delete blob name placeholder
        var blobName = GetElementByTestId(document, "delete-blob-name");
        blobName.Should().NotBeNull();

        // Verify modal buttons
        var cancelButton = GetElementByTestId(document, "delete-modal-cancel");
        cancelButton.Should().NotBeNull().And.HaveTextContent("Cancel");

        var confirmButton = GetElementByTestId(document, "delete-modal-confirm");
        confirmButton.Should().NotBeNull().And.HaveTextContent("Delete");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ShouldIncludeInfoPanel()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        var panel = GetElementByTestId(document, "blob-info-panel");
        panel.Should().NotBeNull();
        panel!.GetAttribute("id").Should().Be("blobInfoPanel");
        panel.ClassList.Contains("offcanvas").Should().BeTrue();
        panel.ClassList.Contains("offcanvas-end").Should().BeTrue();

        // Verify panel content area
        var content = GetElementByTestId(document, "blob-info-content");
        content.Should().NotBeNull();
    }

    #endregion

    #region Load More Tests

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ShouldIncludeLoadMoreButton()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
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
    public async Task ContainerPage_ShouldIncludeJavaScriptForDynamicLoading()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify key JavaScript functions are present
        html.Should().Contain("loadBlobs");
        html.Should().Contain("renderBlobs");
        html.Should().Contain("handleSort");
        html.Should().Contain("showDeleteModal");
        html.Should().Contain("showInfoPanel");
        html.Should().Contain("handleUpload");
        html.Should().Contain("handleDeleteBlob");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ShouldIncludeUtilityFunctions()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify utility functions are present
        html.Should().Contain("formatRelativeTime");
        html.Should().Contain("formatFileSize");
        html.Should().Contain("escapeHtml");
        html.Should().Contain("buildApiUrl");
        html.Should().Contain("getContentTypeIcon");
    }

    #endregion

    #region Integration with API Tests

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_WithNoBlobs_ApiShouldReturnEmptyList()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act - Call the API that the page will use
        var apiResponse = await client.GetAsync($"/api/containers/{containerName}/blobs?$top=25&$orderby=name");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<BlobDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().BeEmpty();
        data.TotalCount.Should().Be(0);
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_WithBlobs_ApiShouldReturnBlobsList()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob1.txt", "content1");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob2.txt", "content2");
        await Fixture.Azurite.CreateBlobAsync(containerName, "blob3.txt", "content3");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act - Call the API that the page will use
        var apiResponse = await client.GetAsync($"/api/containers/{containerName}/blobs?$top=25&$orderby=name");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<BlobDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().HaveCount(3);
        data.TotalCount.Should().Be(3);

        // Verify blobs are sorted by name
        var blobNames = data.Items.Select(b => b.Name).ToList();
        blobNames.Should().BeInAscendingOrder();
        blobNames.Should().Contain("blob1.txt");
        blobNames.Should().Contain("blob2.txt");
        blobNames.Should().Contain("blob3.txt");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ApiWithDescendingSort_ShouldReturnSortedBlobs()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(containerName, "aaa-blob.txt", "content1");
        await Fixture.Azurite.CreateBlobAsync(containerName, "bbb-blob.txt", "content2");
        await Fixture.Azurite.CreateBlobAsync(containerName, "ccc-blob.txt", "content3");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act - Call the API with descending sort
        var apiResponse = await client.GetAsync($"/api/containers/{containerName}/blobs?$top=25&$orderby=name desc");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<BlobDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().HaveCount(3);

        // Verify blobs are sorted descending by name
        var blobNames = data.Items.Select(b => b.Name).ToList();
        blobNames.Should().BeInDescendingOrder();
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ApiWithPagination_ShouldReturnNextLink()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        for (int i = 1; i <= 30; i++)
        {
            await Fixture.Azurite.CreateBlobAsync(containerName, $"blob-{i:D3}.txt", $"content{i}");
        }
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act - Get first page
        var apiResponse = await client.GetAsync($"/api/containers/{containerName}/blobs?$top=25&$orderby=name");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<BlobDTO>>(ServiceFixture.JsonOptions);

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
    public async Task ContainerPage_ApiWithBlobs_ShouldReturnBlobProperties()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(containerName, "test-blob.txt", "Test content");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var apiResponse = await client.GetAsync($"/api/containers/{containerName}/blobs?$top=25&$orderby=name");
        var data = await apiResponse.Content.ReadFromJsonAsync<PagedResponse<BlobDTO>>(ServiceFixture.JsonOptions);

        // Assert
        apiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        data.Should().NotBeNull();
        data!.Items.Should().HaveCount(1);

        var blob = data.Items.First();
        blob.Name.Should().Be("test-blob.txt");
        blob.ContainerName.Should().Be(containerName);
        blob.ContentType.Should().Be("text/plain");
        blob.ContentLength.Should().BeGreaterThan(0);
        blob.ETag.Should().NotBeNullOrEmpty();
        blob.LastModified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_WithNonExistentContainer_ShouldStillRender()
    {
        // Arrange
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/containers/non-existent-container");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify page still renders with correct container name in breadcrumb
        var breadcrumb = GetElementByTestId(document, "breadcrumb-container");
        breadcrumb.Should().NotBeNull();
        breadcrumb!.TextContent.Trim().Should().Be("non-existent-container");

        // Verify table is present
        var table = GetElementByTestId(document, "blobs-table");
        table.Should().NotBeNull();
    }

    #endregion

    #region Page Title Tests

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();
        var document = await ParseHtmlAsync(html);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var title = document.QuerySelector("title");
        title.Should().NotBeNull();
        title!.TextContent.Should().Contain("Blobs");
        title.TextContent.Should().Contain(containerName);
    }

    #endregion

    #region Blob Query Parameter Tests

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_WithBlobQueryParameter_ShouldIncludeBlobNameInScript()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.Azurite.CreateBlobAsync(containerName, "highlight-me.txt", "content");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}?blob=highlight-me.txt");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the blob name is included in the JavaScript
        html.Should().Contain("highlightBlobName");
        html.Should().Contain("highlight-me.txt");
    }

    [Fact(Timeout = 60000)]
    public async Task ContainerPage_WithoutBlobQueryParameter_ShouldHaveEmptyHighlightBlobName()
    {
        // Arrange
        var containerName = await Fixture.Azurite.CreateContainerAsync("test-container");
        await Fixture.SynchronizeCacheAsync();
        using var client = Fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/containers/{containerName}");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify highlightBlobName is empty
        html.Should().Contain("highlightBlobName");
        html.Should().Match("*highlightBlobName = '';*");
    }

    #endregion
}
