# Writing API Tests

An API test is a special kind of integration test that tests the external API of the service using a HTTP client.

Read the [Writing Integration Tests](./WritingIntegrationTests.spec.md) document before beginning.

The API tests are placed in the `tests/AzuriteUI.Web.IntegrationTests` project, and follow a naming scheme `API/{controller}_{endpoint_name}_Tests.cs` - here `{controller}` is the controller under test and `{endpoint_name}` is the name of the endpoint.  For example, if you were writing tests for the `ListContainers` endpoint in class `Controllers/StorageController.cs` in the AzuriteUI.Web project, you would place your test class in `API/StorageController_ListEndpoints_Tests.cs` within the `AzuriteUI.Web.IntegrationTests` project.

## General rules

* Group tests for a single method or property within regions.
* Use AAA (Arrange-Act-Assert) format.
* Decorate the test class with `[ExcludeFromCodeCoverage]`
* Use AwesomeAssertions, which is API compatible with FluentAssertions.
* Use the `ServiceFixture` to spin up a `WebApplicationFactory{T}` compatible service endpoint.
* Do not mock.
* **DO NOT USE Moq or FluentAssertions** - these are forbidden libraries due to licensing.
* Do not test log messages.

## Class Inheritence

A typical API test class looks like this:

```csharp
public class StorageController_CreateContainer_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
  // Tests
}
```

The `BaseApiTest` cleans up the cache database and the Azurite instance before the test starts, resulting in a clean setup.

## The ServiceAppFactory and ServiceFixture

The `ServiceFixture` and `ServiceAppFactory` classes are in the `Helpers` directory.  They create a `WebApplicationFactory` for the entire
application, backed by an Azurite test container and a SQLite database.  The "sync queue" (CacheSyncScheduler) is disabled in the version
that is set up by the `ServiceFixture`.  You will have to trigger a sync if required.  Here is some sample code to do that:

```csharp
using var scope = fixture.Services.CreateScope();
var syncService = scope.GetRequiredService<ICacheSyncService>();
await syncService.SynchronizeCacheAsync(cancellationToken);
```

The `ServiceFixture` has a number of helper methods to assist with running tests.

The `ServiceFixture.JsonOptions` is the set of JSON Serilizer options to use for deserializing content from the serve.

### AzuriteFixture

AzuriteFixture allows you to run tests against a real Azurite instance, started as a TestContainer.  It contains several methods that can make writing tests easier:

* BlobExistsAsync(containerName, blobName) - checks if the blob exists in the container.
* CleanupAsync() - removes all containers from the Azurite instance.
* CreateBlobAsync(containerName, blobName, content) - creates a text/plain blob, returning the name.
* CreateContainerAsync(containerName, metadata?) - creates a container, returning the name.

These methods should be used in preference of new helper methods.

You can get a reference to the Azurite fixture by using `serviceFixture.Azurite`.  This allows you to access the whole stack as needed for tests.

## Time outs

Add a 60 second timeout to each test that you write.  We will adjust timeouts as needed if this is too short.

## Exception Handling in API Tests

The `AzuriteExceptionFilter` is registered globally to handle exceptions from the AzuriteService layer and convert them into proper HTTP responses with ProblemDetails bodies. When writing API tests, include test cases for error conditions to verify the exception filter works correctly end-to-end.

### Conditional Request Tests

Each HTTP method is only expected to handle certain conditions for conditional requests:

* GET of multiple items / list operations will not have conditional request support.
* GET of a single item will support `If-None-Match: "etag"`.
* DELETE of a single item will support `If-Match: "etag"`.
* PUT of a single item will support `If-Match: "etag".

Only test these conditions.  Do not test combinations of conditions or non-listed conditions.

### Error Scenarios to Test

When writing API tests for endpoints that interact with Azurite, include tests for the following error scenarios:

* **404 Not Found** - Test requests for non-existent resources (containers, blobs):

  * Verify status code is 404
  * Verify ProblemDetails response structure
  * Verify `resourceName` is included in extensions when applicable

* **409 Conflict** - Test attempts to create resources that already exist:

  * Verify status code is 409
  * Verify ProblemDetails response structure
  * Verify `resourceName` is included in extensions when applicable

* **416 Range Not Satisfiable** - Test invalid byte range requests for blob downloads:

  * Verify status code is 416
  * Verify ProblemDetails response structure

* **503 Service Unavailable** - Test behavior when Azurite is unavailable (optional, may be complex to simulate)

### Example Error Test

```csharp
[Fact(Timeout = 60000)]
public async Task GetContainer_NonExistentContainer_Returns404WithProblemDetails()
{
    // Arrange
    var client = _fixture!.CreateClient();
    var nonExistentContainer = "container-that-does-not-exist-12345";

    // Act
    var response = await client.GetAsync($"/api/containers/{nonExistentContainer}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

    var problemDetails = await response.Content.ReadFromJsonAsync<JsonDocument>();
    problemDetails.Should().NotBeNull();

    var root = problemDetails!.RootElement;
    root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
    root.GetProperty("title").GetString().Should().Be("Not Found");
    root.GetProperty("detail").GetString().Should().Contain("not found");
}
```

### Integration with Endpoint Development

**Important**: Exception handling tests should be integrated into API test classes as endpoints are developed. Do not create a separate `StorageController_ErrorHandling_Tests.cs` class. Instead, include error scenario tests within each endpoint's test class.

For example, when writing tests for a `GetContainer` endpoint:

* Create happy path tests for successful retrieval
* Create error tests for 404 when container doesn't exist
* Group both in the same test class

This approach ensures comprehensive coverage of both success and error paths for each endpoint.
