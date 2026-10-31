using System;

namespace Jellyfin.Plugin.PlaylistTriage.Api.Models;

/// <summary>
/// Response payload after marking current media item into triage playlist.
/// </summary>
public class MarkCurrentResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the marking operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the marked media item ID.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the marked media item name.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target playlist name.
    /// </summary>
    public string PlaylistName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target playlist ID.
    /// </summary>
    public Guid PlaylistId { get; set; }

    /// <summary>
    /// Gets or sets a human-readable feedback message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the toggle action: "added" or "removed".
    /// </summary>
    public string Action { get; set; } = "added";

    /// <summary>
    /// Gets or sets a value indicating whether the item was removed (toggled off).
    /// </summary>
    public bool Removed { get; set; }

    /// <summary>
    /// Gets or sets enriched resolution tag (e.g. 1080p, 4K UHD).
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// Gets or sets formatted file size string (e.g. 2.45 GB).
    /// </summary>
    public string? FormattedSize { get; set; }
}
