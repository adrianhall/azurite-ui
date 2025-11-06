using System.ComponentModel.DataAnnotations;

namespace AzuriteUI.Web.Services.Repositories.Models;

/// <summary>
/// Request DTO for committing an upload session by providing the list of block IDs in the desired order.
/// </summary>
public class CommitUploadRequestDTO
{
    /// <summary>
    /// The list of block IDs to commit, in the order they should appear in the final blob.
    /// Each block ID must have been previously uploaded via UploadBlock.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one block ID must be provided.")]
    public required IEnumerable<string> BlockIds { get; init; }
}
