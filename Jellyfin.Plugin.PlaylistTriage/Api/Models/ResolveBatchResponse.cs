using System.Collections.Generic;

namespace Jellyfin.Plugin.PlaylistTriage.Api.Models;

/// <summary>
/// Response payload after resolving a batch of items.
/// </summary>
public class ResolveBatchResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether batch operation completed without critical errors.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets action that was executed.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets number of items requested for processing.
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// Gets or sets number of items successfully processed.
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Gets or sets total reclaimed disk size in bytes (for delete_file_from_disk).
    /// </summary>
    public long ReclaimedSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets formatted reclaimed disk size string (e.g. 14.2 GB).
    /// </summary>
    public string FormattedReclaimedSize { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any per-item error or warning messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets a human-readable summary message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
