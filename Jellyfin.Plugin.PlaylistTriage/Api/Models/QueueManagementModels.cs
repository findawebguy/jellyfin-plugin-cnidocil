using System;

namespace Jellyfin.Plugin.PlaylistTriage.Api.Models;

/// <summary>
/// Request payload to delete all media items in a triage playlist/queue from disk.
/// </summary>
public class PurgeQueueRequest
{
    /// <summary>
    /// Gets or sets target playlist name to purge (e.g. "Delete Queue").
    /// </summary>
    public string? PlaylistName { get; set; }

    /// <summary>
    /// Gets or sets target playlist ID.
    /// </summary>
    public Guid? PlaylistId { get; set; }

    /// <summary>
    /// Gets or sets calling user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating explicit user confirmation of irreversible file erasure.
    /// </summary>
    public bool ConfirmPurge { get; set; }
}

/// <summary>
/// Request payload to assign a custom icon to a playlist.
/// </summary>
public class AssignIconRequest
{
    /// <summary>
    /// Gets or sets playlist name.
    /// </summary>
    public string PlaylistName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets icon identifier (e.g. "trash", "bookmark", "star", "scissors", "fire", "heart", "archive", "clock", "film", "folder").
    /// </summary>
    public string IconKey { get; set; } = "bookmark";
}

/// <summary>
/// Request payload to create or register a new triage playlist.
/// </summary>
public class CreatePlaylistRequest
{
    /// <summary>
    /// Gets or sets the name of the playlist to create.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon key for the playlist.
    /// </summary>
    public string IconKey { get; set; } = "bookmark";

    /// <summary>
    /// Gets or sets a value indicating whether to pin this playlist to the player OSD bar.
    /// </summary>
    public bool PinToOsd { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to set this playlist as the default hotkey target.
    /// </summary>
    public bool SetAsDefault { get; set; }

    /// <summary>
    /// Gets or sets the calling user ID.
    /// </summary>
    public Guid? UserId { get; set; }
}

/// <summary>
/// Request payload to update settings for an existing playlist (default, pin, icon).
/// </summary>
public class UpdatePlaylistSettingsRequest
{
    /// <summary>
    /// Gets or sets the playlist name.
    /// </summary>
    public string PlaylistName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new icon key (optional).
    /// </summary>
    public string? IconKey { get; set; }

    /// <summary>
    /// Gets or sets whether the playlist is pinned to the player OSD (optional).
    /// </summary>
    public bool? PinToOsd { get; set; }

    /// <summary>
    /// Gets or sets whether this playlist is the default hotkey target (optional).
    /// </summary>
    public bool? SetAsDefault { get; set; }

    /// <summary>
    /// Gets or sets the calling user ID (optional).
    /// </summary>
    public Guid? UserId { get; set; }
}

/// <summary>
/// Request payload to delete/unregister a playlist from triage.
/// </summary>
public class DeletePlaylistRequest
{
    /// <summary>
    /// Gets or sets the playlist name to remove.
    /// </summary>
    public string PlaylistName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional Jellyfin playlist ID.
    /// </summary>
    public Guid? PlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the calling user ID.
    /// </summary>
    public Guid? UserId { get; set; }
}

/// <summary>
/// Request payload to update player OSD and overlay preferences.
/// </summary>
public class OverlayPreferencesRequest
{
    /// <summary>
    /// Gets or sets whether player controls are swapped.
    /// </summary>
    public bool? SwapOsdControls { get; set; }

    /// <summary>
    /// Gets or sets whether the Stop button is hidden.
    /// </summary>
    public bool? HideStopButton { get; set; }

    /// <summary>
    /// Gets or sets whether Previous/Next buttons are hidden.
    /// </summary>
    public bool? HidePrevNextButtons { get; set; }

    /// <summary>
    /// Gets or sets whether the floating quick-triage menu overlay button is shown.
    /// </summary>
    public bool? ShowFloatingQuickOverlay { get; set; }

    /// <summary>
    /// Gets or sets the configured hotkey string.
    /// </summary>
    public string? ConfiguredHotkey { get; set; }

    /// <summary>
    /// Gets or sets the active theme CSS string.
    /// </summary>
    public string? ActiveThemeCss { get; set; }
}
