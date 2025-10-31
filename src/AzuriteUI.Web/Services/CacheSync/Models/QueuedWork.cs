namespace AzuriteUI.Web.Services.CacheSync.Models;

/// <summary>
/// A class representing a unit of queued work.
/// </summary>
public class QueuedWork
{
    /// <summary>
    /// The unique identifier for the queued work.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
}
