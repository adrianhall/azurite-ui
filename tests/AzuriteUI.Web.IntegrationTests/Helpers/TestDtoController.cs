using AzuriteUI.Web.Controllers.Models;
using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzuriteUI.Web.IntegrationTests.Helpers;

/// <summary>
/// Test controller for testing DTO header generation.
/// </summary>
[ApiController]
[Route("api/test/dto")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test controller methods")]
[ExcludeFromCodeCoverage(Justification = "Test controller")]
public class TestDtoController : ControllerBase
{
    internal static readonly DateTimeOffset ContainerLastModified = new(2025, 1, 15, 10, 30, 45, TimeSpan.Zero);
    internal static readonly DateTimeOffset BlobLastModified = new(2025, 2, 20, 14, 15, 30, TimeSpan.Zero);

    /// <summary>
    /// Returns a single ContainerDTO for testing header generation.
    /// </summary>
    [HttpGet("container")]
    public IActionResult GetContainer()
    {
        var dto = new ContainerDTO
        {
            Name = "test-container",
            ETag = "test-etag-123",
            LastModified = ContainerLastModified,
            BlobCount = 5,
            TotalSize = 1024,
            PublicAccess = "none"
        };

        return Ok(dto);
    }

    /// <summary>
    /// Returns a single BlobDTO for testing header generation.
    /// </summary>
    [HttpGet("blob")]
    public IActionResult GetBlob()
    {
        var dto = new BlobDTO
        {
            Name = "test-blob.txt",
            ETag = "blob-etag-456",
            LastModified = BlobLastModified,
            ContainerName = "test-container",
            ContentType = "text/plain",
            ContentLength = 512
        };

        return Ok(dto);
    }

    /// <summary>
    /// Returns a PagedResponse of ContainerDTOs for testing collection handling.
    /// </summary>
    [HttpGet("containers")]
    public IActionResult GetContainers()
    {
        var containers = new List<ContainerDTO>
        {
            new ContainerDTO
            {
                Name = "container1",
                ETag = "etag1",
                LastModified = ContainerLastModified,
                BlobCount = 10,
                TotalSize = 2048,
                PublicAccess = "none"
            },
            new ContainerDTO
            {
                Name = "container2",
                ETag = "etag2",
                LastModified = ContainerLastModified,
                BlobCount = 5,
                TotalSize = 1024,
                PublicAccess = "blob"
            }
        };

        var response = new PagedResponse<ContainerDTO>
        {
            Items = containers,
            TotalCount = 2,
            FilteredCount = 2
        };

        return Ok(response);
    }
}
