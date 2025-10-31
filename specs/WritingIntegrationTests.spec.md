# Writing Integration Tests

The integration tests are placed in the `tests/AzuriteUI.Web.IntegrationTests` project, and follow a naming scheme `{folder}/{class}_Tests.cs` - here `{folder}` is the folder name of the class under test and `{class}` is the class under test.  For example, if you were writing tests for class `Services/Azurite/AzuriteService.cs` in the AzuriteUI.Web project, you would place your test class in `Services/Azurite/AzuriteService_Tests.cs` within the `AzuriteUI.Web.IntegrationTests` project.

## General rules

* Group tests for a single method or property within regions.
* Use AAA (Arrange-Act-Assert) format.
* Decorate the test class with `[ExcludeFromCodeCoverage]`
* Use AwesomeAssertions, which is API compatible with FluentAssertions.
* Use NSubstitute for mocking.
* **DO NOT USE Moq or FluentAssertions** - these are forbidden libraries due to licensing.

### Testing log messages

* Avoid testing exact error messages within logs.
* Do not write tests that test debug log output.
* Use Microsoft.Extensions.Diagnostics.Testing TestLogger classes for checking logs.

### Mocking IConfiguration and IServiceProvider

* Do not mock IConfiguration or IServiceProvider
* Build a concrete IConfiguration using a ConfigurationBuilder with an in-memory provider
* Build a concrete IServiceProvider using a ServiceCollection when required

## Time outs

Add a 60 second timeout to each test that you write.  We will adjust timeouts as needed if this is too short.

## Fixtures

Fixtures are located in the `Helpers` directory.  

### AzuriteFixture

AzuriteFixture allows you to run tests against a real Azurite instance, started as a TestContainer.  It contains several methods that can make writing tests easier:

* BlobExistsAsync(containerName, blobName) - checks if the blob exists in the container.
* CleanupAsync() - removes all containers from the Azurite instance.
* CreateBlobAsync(containerName, blobName, content) - creates a text/plain blob, returning the name.
* CreateContainerAsync(containerName, metadata?) - creates a container, returning the name.

These methods should be used in preference of new helper methods.
