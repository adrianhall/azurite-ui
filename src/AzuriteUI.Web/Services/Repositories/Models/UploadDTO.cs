using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// A data transfer object for listing in-progress uploads.
/// </summary>
public class UploadDTO
{
    /// <summary>
    /// The unique identifier for the upload session.
    /// </summary>
    [property: Required]
    [property: ReadOnly(true)]
    [property: Description("The unique identifier for the upload session")]
    public required Guid Id { get; set; }

    /// <summary>
    /// The name of the blob being uploaded.
    /// </summary>
    [property: Required]
    [property: Description("The name of the blob being uploaded")]
    public required string Name { get; set; }

    /// <summary>
    /// The name of the container where the blob will be created.
    /// </summary>
    [property: Required]
    [property: Description("The name of the container where the blob will be created")]
    public required string ContainerName { get; set; }

    /// <summary>
    /// The date/time of the last activity (upload, status check, etc.).
    /// </summary>
    [property: Description("The date/time of the last activity")]
    public DateTimeOffset LastActivityAt { get; set; }

    /// <summary>
    /// The upload progress as a percentage (0-100).
    /// </summary>
    [property: Description("The upload progress as a percentage (0-100)")]
    public double Progress { get; set; }
}