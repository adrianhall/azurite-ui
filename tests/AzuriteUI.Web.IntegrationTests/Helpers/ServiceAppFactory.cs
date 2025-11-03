using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzuriteUI.Web.IntegrationTests.Helpers;

/// <summary>
/// A version of the <see cref="WebApplicationFactory{TStartup}"/> that allows setting configuration values.
/// </summary>
/// <param name="settings">The settings.</param>
[ExcludeFromCodeCoverage(Justification = "Test fixture")]
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

        builder.UseEnvironment("Test");
    }
}