using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PlaylistTriage.Configuration;

/// <summary>
/// Plugin configuration for Playlist Triage, Library Pruning, and Custom Player Controls.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        DefaultPlaylistName = "Triage Queue";
        ConfiguredHotkey = "Ctrl+Shift+B";
        RequireConfirmationForPurge = true;
        ShowSuccessToast = true;
        ToastDurationMs = 2500;
        AutoCreatePlaylistIfMissing = true;
        SwapOsdControls = false;
        HideStopButton = false;
        HidePrevNextButtons = false;
        ShowFloatingQuickOverlay = true;
        ActiveThemeCss = string.Empty;

        QuickAccessPlaylists = new List<string>
        {
            "Triage Queue",
            "Delete Queue",
            "Watch Later"
        };

        PlaylistIconMappings = new List<PlaylistIconMapping>
        {
            new() { PlaylistName = "Triage Queue", IconKey = "bookmark" },
            new() { PlaylistName = "Delete Queue", IconKey = "trash" },
            new() { PlaylistName = "Watch Later", IconKey = "clock" },
            new() { PlaylistName = "Favorites", IconKey = "star" }
        };
    }

    /// <summary>
    /// Gets or sets the default target playlist name (default: "Triage Queue").
    /// </summary>
    public string DefaultPlaylistName { get; set; }

    /// <summary>
    /// Gets or sets the configured keyboard hotkey combination (e.g. "Ctrl+Shift+B", "Alt+D").
    /// </summary>
    public string ConfiguredHotkey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether file purge from disk requires explicit two-step confirmation.
    /// </summary>
    public bool RequireConfirmationForPurge { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show visual feedback toast on marking.
    /// </summary>
    public bool ShowSuccessToast { get; set; }

    /// <summary>
    /// Gets or sets the visual feedback toast duration in milliseconds.
    /// </summary>
    public int ToastDurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the target playlist should be created automatically if not found.
    /// </summary>
    public bool AutoCreatePlaylistIfMissing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether player play/media controls are swapped to the right and volume slider to the left.
    /// </summary>
    public bool SwapOsdControls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Stop button is hidden in the video player OSD.
    /// </summary>
    public bool HideStopButton { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Previous/Next track buttons are hidden in the video player OSD.
    /// </summary>
    public bool HidePrevNextButtons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the floating quick-triage menu overlay pill is visible across Jellyfin.
    /// </summary>
    public bool ShowFloatingQuickOverlay { get; set; }

    /// <summary>
    /// Gets or sets the currently active custom theme CSS or preset URL applied by the plugin.
    /// </summary>
    public string ActiveThemeCss { get; set; }

    /// <summary>
    /// Gets or sets the list of playlist names that appear as quick 1-click icon buttons in the player OSD.
    /// </summary>
    public List<string> QuickAccessPlaylists { get; set; }

    /// <summary>
    /// Gets or sets the custom icon mappings and registered playlists.
    /// </summary>
    public List<PlaylistIconMapping> PlaylistIconMappings { get; set; }
}

/// <summary>
/// Mapping between a playlist name and its UI icon key.
/// </summary>
public class PlaylistIconMapping
{
    /// <summary>
    /// Gets or sets playlist name.
    /// </summary>
    public string PlaylistName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets icon key (e.g. "trash", "bookmark", "star", "scissors", "fire", "heart", "archive", "clock", "film", "folder").
    /// </summary>
    public string IconKey { get; set; } = "bookmark";
}
