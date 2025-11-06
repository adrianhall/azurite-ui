using Microsoft.Net.Http.Headers;

namespace AzuriteUI.Web.Extensions;

/// <summary>
/// Extension methods for system types.
/// </summary>
public static class SystemExtensions
{
    /// <summary>
    /// Determines whether the given ETag header matches the specified ETag value.
    /// </summary>
    /// <remarks>
    /// An ETag is considered a match if the tags are identical, ignoring surrounding quotes.
    /// </remarks>
    /// <param name="etag">The ETag header value</param>
    /// <param name="eTag">The ETag value to match against</param>
    /// <returns>True if the ETag header matches the specified ETag value; otherwise, false.</returns>
    public static bool Matches(this EntityTagHeaderValue etag, string eTag)
        => !etag.IsWeak
        && etag.Tag.Length > 0
        && (etag.Tag == "*" || etag.Tag.ToString().Trim('"').Equals(eTag.Trim('"'), StringComparison.Ordinal));

    /// <summary>
    /// Sets the value to the default if the value is null or whitespace.
    /// </summary>
    /// <param name="value">The original value.</param>
    /// <param name="defaultValue">The default value to use if the original value is null or whitespace.</param>
    /// <returns>The original value if it is not null or whitespace; otherwise, the default value.</returns>
    public static string OrDefault(this string? value, string defaultValue)
        => string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}