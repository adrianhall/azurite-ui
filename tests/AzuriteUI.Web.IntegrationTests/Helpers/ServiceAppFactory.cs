using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AzuriteUI.Web.IntegrationTests.Helpers;

/// <summary>
/// A version of the <see cref="WebApplicationFactory{TStartup}"/> that allows setting configuration values.
/// </summary>
/// <param name="settings">The settings.</param>
[ExcludeFromCodeCoverage(Justification = "Test fixture")]
[SuppressMessage("Style", "IDE0053:Use expression body for lambda expression", Justification = "Readability")]
public class ServiceAppFactory(IDictionary<string, string>? settings = null) : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        if (settings is not null)
        {
            foreach (var kvp in settings)
            {
                builder.UseSetting(kvp.Key, kvp.Value);
            }
        }

        // Add test controllers from the test assembly.
        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TestExceptionController).Assembly);
        });

        builder.UseEnvironment("Test");
    }
}