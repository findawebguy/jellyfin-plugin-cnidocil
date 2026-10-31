using System;

namespace Jellyfin.Plugin.PlaylistTriage.Api.Models;

/// <summary>
/// Request payload for marking the currently playing media item.
/// </summary>
public class MarkCurrentRequest
{
    /// <summary>
    /// Gets or sets the target playlist name. If omitted, uses default configured playlist ("Vidz").
    /// </summary>
    public string? PlaylistName { get; set; }

    /// <summary>
    /// Gets or sets an explicit user ID if calling from client context.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets an explicit media item ID if known by caller.
    /// </summary>
    public Guid? ItemId { get; set; }

    /// <summary>
    /// Gets or sets client device ID to pinpoint exact playback session.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets client session ID to pinpoint exact playback session.
    /// </summary>
    public string? SessionId { get; set; }
}
