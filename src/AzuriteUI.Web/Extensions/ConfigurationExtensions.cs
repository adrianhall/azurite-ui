namespace AzuriteUI.Web.Extensions;

/// <summary>
/// A set of extension methods for <see cref="IConfiguration"/>.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Retrieves a required connection string from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration containing the connection strings.</param>
    /// <param name="name">The name of the connection string.</param>
    /// <returns>The value of the connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string does not exist in the configuration.</exception>
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' is not configured.");
        }
        return connectionString;
    }
}
