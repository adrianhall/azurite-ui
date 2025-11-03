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
