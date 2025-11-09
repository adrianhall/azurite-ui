using System.Net;
using System.Text.Json;

namespace AzuriteUI.Web.IntegrationTests.API;

[ExcludeFromCodeCoverage(Justification = "API Test class")]
public class ActionsController_SyncCache_Tests(ServiceFixture fixture) : BaseApiTest(fixture)
{
    #region Happy Path Tests

    [Fact(Timeout = 60000)]
    public async Task SyncCache_ShouldEnqueueWorkInQueue()
    {
        // Arrange
        await Fixture.ClearCacheSyncQueueAsync();
        using HttpClient client = Fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/api/actions/sync-cache", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var jsonContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CacheSyncResult>(jsonContent, ServiceFixture.JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        var cacheQueue = Fixture.CacheSyncQueue.ToList();
        Fixture.CacheSyncQueue.Should().ContainSingle(x => x.Id == result.Id);
    }

    #endregion
    
    #region Helper Classes
    class CacheSyncResult
    {
        public Guid Id { get; set; }
    }
    #endregion
}
