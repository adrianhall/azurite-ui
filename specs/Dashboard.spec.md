# Front page Dashboard Support

The API `GET /api/dashboard` returns information about the dashboard.  This consists of four totals plus two lists.

Example result:

```json
{
    "stats": {
        "containers": 0,
        "blobs": 0,
        "totalBlobSize": 0,
        "totalImageSize": 0
    },
    "recentContainers": [
        { /* info 1 */ },
        { /* info 2 */ },
        { /* info 3 */ },
        { /* info 4 */ },
        { /* info 5 */ },
        { /* info 6 */ },
        { /* info 7 */ },
        { /* info 8 */ },
        { /* info 9 */ }
    ],
    "recentBlobs": [
        { /* info 1 */ },
        { /* info 2 */ },
        { /* info 3 */ },
        { /* info 4 */ },
        { /* info 5 */ },
        { /* info 6 */ },
        { /* info 7 */ },
        { /* info 8 */ },
        { /* info 9 */ }       
    ]
}
```

Here, the `info` depends on containers or blobs.  For containers, the following information is provided:

* Container Name
* Last Modified Date for the most recent blob change or container last-modified (whichever is newer)
* Number of blobs
* Total size of blobs

For blobs, the following information is provided:

* Blob Name
* Container Name
* Last modified date
* Content type
* Content length

## Claude’s Plan

### Dashboard API Implementation Plan

1. Create Response DTOs (src/AzuriteUI.Web/Controllers/Models/)
    * DashboardResponse.cs - Main response with Stats, RecentContainers, RecentBlobs properties
    * DashboardStats.cs - Stats object (Containers, Blobs, TotalBlobSize, TotalImageSize)
    * RecentContainerInfo.cs - Container info (Name, LastModified, BlobCount, TotalSize)
    * RecentBlobInfo.cs - Blob info (Name, ContainerName, LastModified, ContentType, ContentLength)
    * All properties with [Required], [Description] attributes following existing patterns

2. Extend Repository Interface (src/AzuriteUI.Web/Services/Repositories/)
    * IStorageRepository.cs - Add method: `Task<DashboardResponse> GetDashboardDataAsync(CancellationToken cancellationToken = default);`

3. Implement Repository Method (src/AzuriteUI.Web/Services/Repositories/)
    * StorageRepository.cs - Implement GetDashboardDataAsync:
    * Query stats: count containers/blobs, sum blob sizes, sum image sizes
    * Query recent blobs: order by LastModified desc, take 10
    * Query recent containers with complex logic:
    * Join containers with their blobs
    * Compute max(container.LastModified, max(blob.LastModified per container))
    * Order by computed lastModified desc, take 10
    * Return DashboardResponse

4. Create Dashboard Controller (src/AzuriteUI.Web/Controllers/)
    * DashboardController.cs - Partial controller base with DI (IStorageRepository, ILogger)
    * DashboardController.GetDashboard.cs - GET /api/dashboard endpoint:
    * Log the operation
    * Call repository.GetDashboardDataAsync()
    * Return Ok(response)
    * ProducesResponseType, EndpointName, EndpointDescription attributes

5. Update API Specification (specs/)
    * API.spec.md - Add new section "GetDashboard: GET /api/dashboard"
    * Request format (no parameters)
    * Response format with full JSON schema
    * Status codes (200 OK, 400 Bad Request)
    * Response headers (Content-Type)

6. Write API Integration Tests (tests/AzuriteUI.Web.IntegrationTests/API/)
    * DashboardController_GetDashboard_Tests.cs (inherits BaseApiTest):
    * Test 0 containers, 0 blobs: verify empty dashboard with all zeros
    * Test 5 containers, 5 blobs: verify all returned, stats correct
    * Test 15 containers, 15 blobs: verify only 10 of each returned, stats include all 15
    * Test recent lists ordered by lastModified descending
    * Test totalImageSize with mix of image/* and other content types
    * Test container lastModified uses max of container and its blob times
    * Test containers without blobs use container.LastModified
    * All tests: 60s timeout, FluentAssertions, use Fixture.Azurite + SynchronizeCacheAsync
    * `[ExcludeFromCodeCoverage]` on class
    * Follow [WritingApiTests.spec.md](./WritingApiTests.spec.md)

7. Write Repository Integration Tests (tests/AzuriteUI.Web.IntegrationTests/Services/Repositories/)
    * StorageRepository_GetDashboardData_Tests.cs (new class):
    * Use AzuriteFixture and real CacheDbContext (follow existing StorageRepository integration test patterns)
    * Test GetDashboardDataAsync with 0 containers
    * Test GetDashboardDataAsync with 10 containers, 10 blobs
    * Test GetDashboardDataAsync with 20 containers, 20 blobs (verify only 10 returned)
    * Test complex lastModified computation for containers
    * Test image size calculation separately
    * Test blobs ordering and limiting
    * All tests: 60s timeout, AAA format, AwesomeAssertions
    * `[ExcludeFromCodeCoverage]` on class
    * Use regions to group tests
    * Follow [WritingIntegrationTests.spec.md](./WritingIntegrationTests.spec.md)

8. Write Repository Unit Tests (tests/AzuriteUI.Web.UnitTests/Services/Repositories/)
    * StorageRepository_GetDashboardData_Tests.cs (new class):
    * Use in-memory SQLite database (follow existing unit test patterns)
    * Mock ILogger with NSubstitute
    * Test GetDashboardDataAsync with various scenarios:
    * Empty database
    * Various counts and sizes
    * Image content type filtering
    * Container lastModified calculation edge cases
    * Proper ordering and limiting
    * All tests: 15s timeout, AAA format, AwesomeAssertions
    * `[ExcludeFromCodeCoverage]` on class
    * Use regions to group tests
    * Follow [WritingUnitTests.spec.md](./WritingUnitTests.spec.md)

9. Run Full Test Suite & Coverage
    * Run: dotnet cake --target=Test
    * Analyze: artifacts/coverage/lcov.info
    * Report coverage for new StorageRepository method and DashboardController
    * Ensure all new code is covered

### Test Scenarios Summary

Edge Cases to Cover:

* Empty database (0 containers, 0 blobs)
* Below threshold (5 containers, 5 blobs)
* At threshold (10 containers, 10 blobs)
* Above threshold (15-20 containers, 15-20 blobs) - verify only 10 returned
* Mixed content types (images vs non-images)
* Containers with no blobs (use container.LastModified)
* Containers with blobs where blob.LastModified > container.LastModified
* Containers with blobs where container.LastModified > blob.LastModified
* Proper descending order by lastModified

Key Implementation Details:

* Follow partial class pattern for controller
* Use async/await with CancellationToken throughout
* Use LINQ with EF Core (Count, Sum, Take, OrderByDescending, GroupBy/Join)
* Handle null/empty cases gracefully
* Use proper logging with structured parameters
* Follow JSON naming conventions (camelCase)
* Use AwesomeAssertions (not FluentAssertions) in tests
* Use NSubstitute (not Moq) for mocking in unit tests
