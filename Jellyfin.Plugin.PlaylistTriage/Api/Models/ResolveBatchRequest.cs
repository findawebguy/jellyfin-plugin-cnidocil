using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.PlaylistTriage.Api.Models;

/// <summary>
/// Request payload for resolving batch triage items.
/// </summary>
public class ResolveBatchRequest
{
    /// <summary>
    /// Gets or sets triage action: "remove_from_playlist" or "delete_file_from_disk".
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets playlist name from which items should be removed.
    /// </summary>
    public string? PlaylistName { get; set; }

    /// <summary>
    /// Gets or sets playlist ID from which items should be removed.
    /// </summary>
    public Guid? PlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the target items to be resolved.
    /// </summary>
    public List<BatchItemTarget> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets explicit calling user ID.
    /// </summary>
    public Guid? UserId { get; set; }
}

/// <summary>
/// Individual target item identifier within a batch resolution request.
/// </summary>
public class BatchItemTarget
{
    /// <summary>
    /// Gets or sets media item ID.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets linked playlist entry ID (optional).
    /// </summary>
    public string? PlaylistItemId { get; set; }
}
