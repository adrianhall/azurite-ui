namespace AzuriteUI.Web.Services.Display;

/// <summary>
/// Helper methods for displaying data in the UI.
/// </summary>
public interface IDisplayHelper
{
    /// <summary>
    /// Converts a MIME content type to a Bootstrap icon name.
    /// </summary>
    /// <param name="contentType">The content-type to convert.</param>
    /// <returns>The bootstrap-icon name.</returns>
    string ConvertToBootstrapIcon(string contentType);

    /// <summary>
    /// Converts a file count to a human-readable string (e.g., "1 file", "2 files").
    /// </summary>
    /// <param name="fileCount">The file count</param>
    /// <returns>The converted string.</returns>
    string ConvertToFileCount(int fileCount);

    /// <summary>
    /// Converts a byte count to a human-readable string (e.g., "1.5 MB").
    /// </summary>
    /// <param name="byteCount">The byte count to convert.</param>
    /// <returns>A human-readable string representation of the byte count.</returns>
    string ConvertToFileSize(long byteCount);

    /// <summary>
    /// Converts a DateTimeOffset to a human-readable relative time string.
    /// </summary>
    /// <param name="timestamp">The <see cref="DateTimeOffset"/> to convert.</param>
    /// <returns>A relative timestamp</returns>
    string ConvertToRelativeTime(DateTimeOffset timestamp);
}