using AzuriteUI.Web.Services.Azurite;
using AzuriteUI.Web.Services.CacheDb;
using AzuriteUI.Web.Services.CacheSync;
using AzuriteUI.Web.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace AzuriteUI.Web.IntegrationTests;

[ExcludeFromCodeCoverage(Justification = "Test class")]
public class ServiceInitialization_Tests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    [Fact(Timeout = 60000)]
    public async Task CacheDbContext_IsRegisteredAndInitialized()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetService<CacheDbContext>();

        // Act
        context.Should().NotBeNull();

        var schemaVersions = await context!.SchemaVersions.ToListAsync();

        // Assert
        schemaVersions.Should().ContainSingle().And.ContainSingle(x => x.SchemaVersionId == CacheDbInitializer.CurrentSchemaVersion);
    }

    [Fact(Timeout = 60000)]
    public async Task AzuriteService_IsRegistered()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<IAzuriteService>();

        // Assert
        svc.Should().NotBeNull().And.BeAssignableTo<AzuriteService>();
        (svc as AzuriteService)!.ConnectionString.Should().Be(fixture.Azurite.ConnectionString);
    }

    [Theory(Timeout = 60000)]
    [InlineData(typeof(TimeProvider), typeof(TimeProvider))]
    [InlineData(typeof(ICacheSyncService), typeof(CacheSyncService))]
    [InlineData(typeof(IQueueWorker), typeof(QueueWorker))]
    [InlineData(typeof(IQueueManager), typeof(QueueManager))]
    [InlineData(typeof(IStorageRepository), typeof(StorageRepository))]
    public async Task RequiredServices_AreRegistered(Type serviceType, Type implementationType)
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService(serviceType);

        // Assert
        svc.Should().NotBeNull().And.BeAssignableTo(implementationType);
    }

    [Fact(Timeout = 60000)]
    public async Task IEdmModel_IsRegistered()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var model = scope.ServiceProvider.GetService<IEdmModel>();

        // Assert
        model.Should().NotBeNull();
        model.SchemaElements.Should().ContainSingle(e => e.Name == "ContainerDTO");
        model.SchemaElements.Should().ContainSingle(e => e.Name == "BlobDTO");
        model.SchemaElements.Should().ContainSingle(e => e.Name == "UploadDTO");
    }

    [Fact(Timeout = 60000)]
    public async Task HealthEndpoint_IsReachable()
    {
        // Arrange
        using HttpClient client = fixture.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/health");
        var jsonResponse = await response.Content.ReadAsStringAsync();

        // Assert 
        response.EnsureSuccessStatusCode();
        // The unit tests take care of individual responses - the actual response can change over time
        // based on the HealthCheck API for ASP.NET Core, which is out of our control.
    }
}
