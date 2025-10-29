using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Globalization;

namespace AzuriteUI.Web.Services.CacheDb.Converters;

/// <summary>
/// A converter for converting DateTimeOffset to and from a string for database storage. 
/// This is normally used by Sqlite databases to preserve ordering/search requirements.
/// </summary>
public class DateTimeOffsetConverter : ValueConverter<DateTimeOffset, string>
{
    /// <summary>
    /// The format string to use for ISO 8601 UTC representation.
    /// </summary>
    internal const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffZ";

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeOffsetConverter"/> class.
    /// </summary>
    public DateTimeOffsetConverter() : base(
        dto => dto.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture),   // Convert DateTimeOffset to ISO 8601 string for storage
        str => DateTimeOffset.Parse(str))             // Convert ISO 8601 string back to DateTimeOffset
    {
    }
}