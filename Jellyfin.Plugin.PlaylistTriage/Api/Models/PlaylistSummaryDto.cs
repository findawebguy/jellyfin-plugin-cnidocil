using System;

namespace Jellyfin.Plugin.PlaylistTriage.Api.Models;

/// <summary>
/// Summary DTO for user playlists with configured icon and pin/default status.
/// </summary>
public class PlaylistSummaryDto
{
    /// <summary>
    /// Gets or sets playlist ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets playlist display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets total items count in playlist.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets configured icon key (e.g. "trash", "bookmark", "star").
    /// </summary>
    public string IconKey { get; set; } = "bookmark";

    /// <summary>
    /// Gets or sets a value indicating whether this playlist is the default hotkey target.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this playlist is pinned to the player OSD bar.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets the list of item IDs contained in this playlist.
    /// </summary>
    public System.Collections.Generic.List<Guid> ItemIds { get; set; } = new();
}
