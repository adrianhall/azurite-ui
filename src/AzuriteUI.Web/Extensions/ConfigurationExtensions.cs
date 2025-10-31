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
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException($"Connection string '{name}' is not configured.")
            : connectionString;
    }

    /// <summary>
    /// Retrieves a TimeSpan value from the configuration by key.
    /// </summary>
    /// <param name="configuration">The configuration to query.</param>
    /// <param name="key">The key of the TimeSpan value.</param>
    /// <returns>The TimeSpan value, or null if not found or invalid.</returns>
    public static TimeSpan? GetTimeSpan(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return !string.IsNullOrWhiteSpace(value) && TimeSpan.TryParse(value, out var result) ? result : null;
    }
}
