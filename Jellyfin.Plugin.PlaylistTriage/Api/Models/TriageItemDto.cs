using System;

namespace Jellyfin.Plugin.PlaylistTriage.Api.Models;

/// <summary>
/// Enriched media item metadata for playlist triage management.
/// </summary>
public class TriageItemDto
{
    /// <summary>
    /// Gets or sets the Jellyfin Library Item ID.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the playlist linked entry ID (used to remove from playlist).
    /// </summary>
    public string PlaylistItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item title / media name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the physical file path on disk.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the total media bitrate in bps.
    /// </summary>
    public long? Bitrate { get; set; }

    /// <summary>
    /// Gets or sets formatted bitrate string (e.g. 8.2 Mbps).
    /// </summary>
    public string? FormattedBitrate { get; set; }

    /// <summary>
    /// Gets or sets the video frame width.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the video frame height.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets friendly resolution label (e.g. 4K UHD, 1080p, 720p).
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// Gets or sets video stream codec (e.g. HEVC, H264, AV1).
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets audio stream codec (e.g. AAC, DTS, AC3, FLAC).
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets file container format (e.g. mkv, mp4).
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long? SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets human-readable formatted file size (e.g. 3.4 GB).
    /// </summary>
    public string? FormattedSize { get; set; }

    /// <summary>
    /// Gets or sets runtime duration ticks.
    /// </summary>
    public long? RunTimeTicks { get; set; }

    /// <summary>
    /// Gets or sets formatted runtime duration (e.g. 01:42:15).
    /// </summary>
    public string? FormattedDuration { get; set; }

    /// <summary>
    /// Gets or sets when this media item was added to the Jellyfin library.
    /// </summary>
    public DateTime? DateAdded { get; set; }

    /// <summary>
    /// Gets or sets timestamp when item was flagged/triaged into playlist.
    /// </summary>
    public DateTime? FlagTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the media item type (Movie, Episode, Video, etc.).
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets overview / plot summary.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// Gets or sets primary image tag for poster thumbnail.
    /// </summary>
    public string? PrimaryImageTag { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether current user has authorization to delete this item.
    /// </summary>
    public bool CanDelete { get; set; }
}
