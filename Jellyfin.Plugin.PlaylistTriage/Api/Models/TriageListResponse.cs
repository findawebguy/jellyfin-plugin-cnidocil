using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.PlaylistTriage.Api.Models;

/// <summary>
/// Response payload for GET /TriageList.
/// </summary>
public class TriageListResponse
{
    /// <summary>
    /// Gets or sets target playlist ID.
    /// </summary>
    public Guid PlaylistId { get; set; }

    /// <summary>
    /// Gets or sets target playlist name.
    /// </summary>
    public string PlaylistName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets total count of items in triage list.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets total aggregate size of files in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets formatted total size (e.g. 48.6 GB).
    /// </summary>
    public string FormattedTotalSize { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets list of enriched media items.
    /// </summary>
    public List<TriageItemDto> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether calling user has administrator or deletion privileges.
    /// </summary>
    public bool UserCanDeleteFiles { get; set; }
}
