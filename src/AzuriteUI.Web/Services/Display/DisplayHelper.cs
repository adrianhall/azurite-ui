using Humanizer;

namespace AzuriteUI.Web.Services.Display;

/// <summary>
/// The concrete implementation of <see cref="IDisplayHelper"/>.
/// </summary>
public class DisplayHelper : IDisplayHelper
{
    /// <inheritdoc />
    public string ConvertToBootstrapIcon(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            var ct when ct.StartsWith("image/") => "file-image",
            var ct when ct.StartsWith("video/") => "file-play",
            var ct when ct.StartsWith("audio/") => "file-music",
            var ct when ct.StartsWith("text/") => "file-text",
            var ct when ct.Contains("pdf") => "file-pdf",
            var ct when ct.Contains("zip") || ct.Contains("compressed") => "file-zip",
            var ct when ct.Contains("json") || ct.Contains("xml") => "file-code",
            _ => "file-earmark"
        };
    }

    /// <inheritdoc />
    public string ConvertToFileCount(int fileCount)
        => fileCount.ToString("N0");

    /// <inheritdoc />
    public string ConvertToFileSize(long byteCount)
        => byteCount.Bytes().Humanize();

    /// <inheritdoc />
    public string ConvertToRelativeTime(DateTimeOffset timestamp)
        => timestamp.Humanize();
}