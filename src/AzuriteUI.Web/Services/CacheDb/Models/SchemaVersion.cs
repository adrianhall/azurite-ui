namespace AzuriteUI.Web.Services.CacheDb.Models;

/// <summary>
/// The current schema version.  This is used by the
/// service to determine whether a database migration is needed.
/// </summary>
public class SchemaVersion
{
    /// <summary>
    /// The current version.
    /// </summary>
    public int SchemaVersionId { get; set; } = 0;
}