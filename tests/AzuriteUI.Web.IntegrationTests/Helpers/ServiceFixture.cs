using AzuriteUI.Web.Services.CacheSync;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AzuriteUI.Web.IntegrationTests.Helpers;

/// <summary>
/// A class fixture for integration tests that manages the lifecycle of the service plus its dependencies.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Test fixture")]
public class ServiceFixture : IAsyncLifetime
{
    private readonly Lazy<ServiceAppFactory> _factory;
    private readonly Lazy<AzuriteFixture> _fixture;

    /// <summary>
    /// Creates a new instance of the <see cref="ServiceFixture"/> class.
    /// </summary>
    public ServiceFixture()
    {
        _fixture = new Lazy<AzuriteFixture>(() => new AzuriteFixture());
        _factory = new Lazy<ServiceAppFactory>(CreateFactory);
        CacheDbFile = Path.Combine(Path.GetTempPath(), $"fixture-cache-{Guid.NewGuid():N}.db");
    }

    /// <summary>
    /// The Azurite instance.
    /// </summary>
    public AzuriteFixture Azurite { get => _fixture.Value; }

    /// <summary>
    /// The database file.
    /// </summary>
    public string CacheDbFile { get; }

    /// <summary>
    /// The database connection string to use.
    /// </summary>
    public string CacheDbConnectionString { get => $"Data Source={CacheDbFile};Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True;"; }

    /// <summary>
    /// The factory for accessing the services.
    /// </summary>
    public ServiceAppFactory Factory { get => _factory.Value; }

    /// <summary>
    /// The services.
    /// </summary>
    public IServiceProvider Services { get => Factory.Services; }

    #region IAsyncLifetime Implementation
    /// <summary>
    /// Part of the <see cref="IAsyncLifetime"/>, this initializes the Azurite container.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await Azurite.InitializeAsync();
    }

    /// <summary>
    /// Part of the <see cref="IAsyncLifetime"/>, this disposes of the factory, stops the Azurite container, and cleans up.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // Dispose of the factory so we can ensure the database connection is closed.
        Factory.Dispose();

        // stop and dispose the Azurite container
        await Azurite.DisposeAsync();

        // delete the database file, skipping over any errors since it may be locked.
        try
        {
            File.Delete(CacheDbFile);
        }
        catch
        {
            // ignored
        }

        // Everything else should be clean, so we can suppress the finalizer.
        GC.SuppressFinalize(this);
    }
    #endregion

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> for making requests to the service.
    /// </summary>
    public HttpClient CreateClient()
    {
        var options = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        };
        return Factory.CreateClient(options);
    }

    /// <summary>
    /// Starts the queue worker service.
    /// </summary>
    public async Task StartQueueAsync(CancellationToken cancellationToken = default)
    {
        using var scope = Factory.Services.CreateScope();
        var queueWorker = scope.ServiceProvider.GetRequiredService<IQueueManager>();
        await queueWorker.StartQueueAsync(cancellationToken);
    }

    /// <summary>
    /// An internal method to create the factory.
    /// </summary>
    internal ServiceAppFactory CreateFactory()
    {
        var settings = new Dictionary<string, string>()
        {
            { "ConnectionStrings:Azurite", Azurite.ConnectionString },
            { "ConnectionStrings:CacheDatabase", CacheDbConnectionString }
        };

        return new ServiceAppFactory(settings);
    }
}