using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace AzuriteUI.Web.Services.CacheDb.Converters;

/// <summary>
/// Converts <see cref="IDictionary{TKey,TValue}"/> for string-based key-value pairs to and 
/// from JSON for database storage
/// </summary>
public class DictionaryJsonConverter : ValueConverter<IDictionary<string, string>, string>
{
    /// <summary>
    /// The JSON serializer options to use.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryJsonConverter"/> class.
    /// </summary>
    public DictionaryJsonConverter()
        : base(
            dictionary => JsonSerializer.Serialize(dictionary, Options),
            json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options) ?? new Dictionary<string, string>())
    {
    }
}