using AzuriteUI.Web.IntegrationTests.Helpers;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;

namespace AzuriteUI.Web.IntegrationTests.API;

/// <summary>
/// Integration tests for the DtoHeaderFilter using the TestDtoController
/// to verify automatic header generation for DTO responses.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestDtoController_Tests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    #region Single ContainerDTO Tests

    [Fact(Timeout = 60000)]
    public async Task GetContainer_ReturnsContainerDTOWithAllHeaders()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/dto/container");

        // Assert
        response.Should().Be200Ok()
            .And.Satisfy(r =>
            {
                r.Headers.Should().ContainKey("ETag").WhoseValue.Should().Contain("\"test-etag-123\"");
                r.Content.Headers.Should().ContainKey("Last-Modified").WhoseValue.Should().Contain(TestDtoController.ContainerLastModified.ToString("R"));
                // Link header is not checked yet.
            });
    }

    #endregion

    #region Single BlobDTO Tests

    [Fact(Timeout = 60000)]
    public async Task GetBlob_ReturnsBlobDTOWithAllHeaders()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/dto/blob");

        // Assert
        response.Should().Be200Ok()
            .And.Satisfy(r =>
            {
                r.Headers.Should().ContainKey("ETag").WhoseValue.Should().Contain("\"blob-etag-456\"");
                r.Content.Headers.Should().ContainKey("Last-Modified").WhoseValue.Should().Contain(TestDtoController.BlobLastModified.ToString("R"));
                // Link header is not checked yet.
            });
    }

    #endregion

    #region Collection (PagedResponse) Tests

    [Fact(Timeout = 60000)]
    public async Task GetContainers_PagedResponse_DoesNotHaveETagHeader()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/dto/containers");

        // Assert
        response.Should().Be200Ok()
            .And.Satisfy(r =>
            {
                r.Headers.Should().NotContainKey("ETag");
                r.Headers.Should().NotContainKey("Link");
                r.Content.Headers.Should().NotContainKey("Last-Modified");
            });
    }

    #endregion
}
