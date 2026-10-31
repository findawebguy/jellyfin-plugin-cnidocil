using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.PlaylistTriage.Api.Models;
using Jellyfin.Plugin.PlaylistTriage.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Playlists;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaylistTriage.Api;

/// <summary>
/// API Controller providing REST endpoints for Jellyfin Playlist Triage and Library Pruning.
/// </summary>
[Authorize]
[ApiController]
[Route("Plugins/PlaylistTriage")]
public class TriageController : ControllerBase
{
    private readonly IPlaylistManager _playlistManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ISessionManager _sessionManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<TriageController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TriageController"/> class.
    /// </summary>
    public TriageController(
        IPlaylistManager playlistManager,
        ILibraryManager libraryManager,
        ISessionManager sessionManager,
        IUserManager userManager,
        ILogger<TriageController> logger)
    {
        _playlistManager = playlistManager;
        _libraryManager = libraryManager;
        _sessionManager = sessionManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Identifies active playback session for calling user, reads currently playing media item ID, and adds to target playlist.
    /// </summary>
    [HttpPost("MarkCurrent")]
    [ProducesResponseType(typeof(MarkCurrentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MarkCurrentResponse>> MarkCurrent([FromBody] MarkCurrentRequest? request)
    {
        SetNoCacheHeaders();
        try
        {
            return await MarkCurrentInternalAsync(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkCurrent encountered an unexpected exception for ItemId='{ItemId}', Playlist='{Playlist}': {Message}",
                request?.ItemId, request?.PlaylistName, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Failed to mark item: " + ex.Message
            });
        }
    }

    private async Task<ActionResult<MarkCurrentResponse>> MarkCurrentInternalAsync(MarkCurrentRequest? request)
    {
        var userId = ResolveCallingUserId(request?.UserId);
        var defaultName = Plugin.Instance?.Configuration.DefaultPlaylistName;
        if (string.IsNullOrWhiteSpace(defaultName) ||
            string.Equals(defaultName, "Vidz", StringComparison.OrdinalIgnoreCase) ||
            IsLibraryName(defaultName))
        {
            defaultName = "Triage Queue";
        }

        var targetPlaylistName = !string.IsNullOrWhiteSpace(request?.PlaylistName)
            ? request.PlaylistName.Trim()
            : defaultName;

        if (string.Equals(targetPlaylistName, "Vidz", StringComparison.OrdinalIgnoreCase) ||
            IsLibraryName(targetPlaylistName))
        {
            targetPlaylistName = defaultName;
        }

        // 1. Resolve currently playing media item
        BaseItem? item = null;
        if (request?.ItemId.HasValue == true && request.ItemId.Value != Guid.Empty)
        {
            try
            {
                item = _libraryManager.GetItemById(request.ItemId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve item by ID {ItemId} from LibraryManager", request.ItemId.Value);
            }
        }

        if (item == null)
        {
            var session = FindActivePlaybackSession(userId != Guid.Empty ? userId : null, request?.DeviceId, request?.SessionId);
            if (session?.FullNowPlayingItem != null)
            {
                item = session.FullNowPlayingItem;
            }
            else if (session?.NowPlayingItem != null && session.NowPlayingItem.Id != Guid.Empty)
            {
                try
                {
                    item = _libraryManager.GetItemById(session.NowPlayingItem.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve item by session ID from LibraryManager");
                }
            }
        }

        if (item == null)
        {
            _logger.LogWarning("MarkCurrent failed: No active playback session or valid item ID found for DeviceId='{DeviceId}', UserId='{UserId}'.", request?.DeviceId, request?.UserId);
            return BadRequest(new { message = "No media is currently playing in an active session." });
        }

        // 2. Resolve or create target playlist
        Playlist? targetPlaylist = null;

        if (userId != Guid.Empty)
        {
            try
            {
                var existing = _playlistManager.GetPlaylists(userId).OfType<Playlist>();
                targetPlaylist = existing.FirstOrDefault(p =>
                    string.Equals(p.Name, targetPlaylistName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get playlists for user {UserId}", userId);
            }
        }

        if (targetPlaylist == null)
        {
            targetPlaylist = QueryAllPlaylistsFromLibrarySafe()
                .FirstOrDefault(p => string.Equals(p.Name, targetPlaylistName, StringComparison.OrdinalIgnoreCase));
        }

        Guid playlistGuid = Guid.Empty;
        bool isRemoved = false;
        if (targetPlaylist == null)
        {
            _logger.LogInformation("Target playlist '{PlaylistName}' not found. Creating new playlist for user {UserId}...", targetPlaylistName, userId);

            PlaylistCreationResult? creationResult = null;
            try
            {
                creationResult = await ExecuteWithRetryAsync(async () =>
                    await CreatePlaylistSafeAsync(targetPlaylistName, userId, new[] { item.Id }).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CreatePlaylist encountered exception for '{PlaylistName}', checking library fallback...", targetPlaylistName);
            }

            if (creationResult != null && Guid.TryParse(creationResult.Id, out var parsedId))
            {
                playlistGuid = parsedId;
            }
            else
            {
                // Fallback: check if the playlist was created on disk or indexed
                var newlyCreated = QueryAllPlaylistsFromLibrarySafe()
                    .FirstOrDefault(p => string.Equals(p.Name, targetPlaylistName, StringComparison.OrdinalIgnoreCase));

                if (newlyCreated != null)
                {
                    playlistGuid = newlyCreated.Id;
                    targetPlaylist = newlyCreated;
                }
            }
        }
        else
        {
            playlistGuid = targetPlaylist.Id;
            var manageableItems = GetPlaylistItemsSafe(targetPlaylist);

            var matches = manageableItems.Where(m =>
                (m?.Item2 != null && m.Item2.Id == item.Id) ||
                (m?.Item1?.ItemId.HasValue == true && m.Item1.ItemId.Value == item.Id) ||
                (!string.IsNullOrEmpty(m?.Item1?.Path) && !string.IsNullOrEmpty(item.Path) && string.Equals(m.Item1.Path, item.Path, StringComparison.OrdinalIgnoreCase))).ToList();

            if (matches.Count > 0)
            {
                // Item is already in the queue -> Toggle OFF (remove)
                try
                {
                    await RemoveItemsFromPlaylistInternalAsync(targetPlaylist, matches).ConfigureAwait(false);
                    isRemoved = true;
                    _logger.LogInformation("Toggled: Removed item '{ItemName}' ({ItemId}) from playlist '{PlaylistName}' ({PlaylistId})",
                        item.Name, item.Id, targetPlaylistName, targetPlaylist.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove items from playlist {PlaylistId}", targetPlaylist.Id);
                }
            }
            else
            {
                // Item is not in queue -> Toggle ON (add)
                try
                {
                    await ExecuteWithRetryAsync(async () =>
                    {
                        await AddItemToPlaylistSafeAsync(targetPlaylist.Id, new[] { item.Id }, userId).ConfigureAwait(false);
                        return true;
                    }).ConfigureAwait(false);

                    _logger.LogInformation("Toggled: Added item '{ItemName}' ({ItemId}) to playlist '{PlaylistName}' ({PlaylistId})",
                        item.Name, item.Id, targetPlaylistName, targetPlaylist.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add item {ItemId} to playlist {PlaylistId} via PlaylistManager", item.Id, targetPlaylist.Id);
                }
            }
        }

        // Ensure playlist is registered in plugin configuration
        try
        {
            EnsurePlaylistRegisteredInConfig(targetPlaylistName, null, false, false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update configuration for playlist '{PlaylistName}'", targetPlaylistName);
        }

        // 3. Extract quick metadata for optimistic toast feedback
        var resolution = FormatResolution(item.Width, item.Height);
        var sizeBytes = item.Size ?? GetFileSizeSafe(item.Path);
        var formattedSize = sizeBytes.HasValue ? FormatBytes(sizeBytes.Value) : null;

        var actionText = isRemoved ? "removed" : "added";
        var feedbackMessage = isRemoved
            ? $"Removed \"{item.Name}\" from \"{targetPlaylistName}\""
            : $"Added \"{item.Name}\" to \"{targetPlaylistName}\"";

        return Ok(new MarkCurrentResponse
        {
            Success = true,
            ItemId = item.Id,
            ItemName = item.Name,
            PlaylistName = targetPlaylistName,
            PlaylistId = playlistGuid,
            Action = actionText,
            Removed = isRemoved,
            Message = feedbackMessage,
            Resolution = resolution,
            FormattedSize = formattedSize
        });
    }

    /// <summary>
    /// Returns enriched metadata for items inside the specified triage playlist.
    /// </summary>
    [HttpGet("TriageList")]
    [ProducesResponseType(typeof(TriageListResponse), StatusCodes.Status200OK)]
    public ActionResult<TriageListResponse> GetTriageList([FromQuery] string? playlistName = null, [FromQuery] Guid? userId = null)
    {
        SetNoCacheHeaders();
        try
        {
            return Ok(ExecuteWithRetry(() => GetTriageListInternal(playlistName, userId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve triage list for playlist '{PlaylistName}'. Returning safe empty response.", playlistName);
            return Ok(new TriageListResponse
            {
                PlaylistId = Guid.Empty,
                PlaylistName = playlistName ?? "Triage Queue",
                TotalItems = 0,
                TotalSizeBytes = 0,
                FormattedTotalSize = "0 B",
                Items = new List<TriageItemDto>(),
                UserCanDeleteFiles = false
            });
        }
    }

    private TriageListResponse GetTriageListInternal(string? playlistName, Guid? userId)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var defaultName = config.DefaultPlaylistName;
        if (string.IsNullOrWhiteSpace(defaultName) ||
            string.Equals(defaultName, "Vidz", StringComparison.OrdinalIgnoreCase) ||
            IsLibraryName(defaultName))
        {
            defaultName = "Triage Queue";
        }

        var targetPlaylistName = !string.IsNullOrWhiteSpace(playlistName)
            ? playlistName.Trim()
            : defaultName;

        if (string.Equals(targetPlaylistName, "Vidz", StringComparison.OrdinalIgnoreCase) ||
            IsLibraryName(targetPlaylistName))
        {
            targetPlaylistName = defaultName;
        }

        var resolvedUserId = ResolveCallingUserId(userId);
        var userCanDelete = CheckUserCanDelete(resolvedUserId);

        Playlist? targetPlaylist = null;
        if (resolvedUserId != Guid.Empty)
        {
            try
            {
                var existing = _playlistManager.GetPlaylists(resolvedUserId).OfType<Playlist>();
                targetPlaylist = existing.FirstOrDefault(p =>
                    string.Equals(p.Name, targetPlaylistName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get playlists for user {UserId}", resolvedUserId);
            }
        }

        if (targetPlaylist == null)
        {
            targetPlaylist = QueryAllPlaylistsFromLibrarySafe()
                .FirstOrDefault(p => string.Equals(p.Name, targetPlaylistName, StringComparison.OrdinalIgnoreCase));
        }

        if (targetPlaylist == null)
        {
            return new TriageListResponse
            {
                PlaylistId = Guid.Empty,
                PlaylistName = targetPlaylistName,
                TotalItems = 0,
                TotalSizeBytes = 0,
                FormattedTotalSize = "0 B",
                Items = new List<TriageItemDto>(),
                UserCanDeleteFiles = userCanDelete
            };
        }

        var itemsList = new List<TriageItemDto>();
        long totalSizeBytes = 0;

        var manageableEntries = GetPlaylistItemsSafe(targetPlaylist);

        foreach (var tuple in manageableEntries)
        {
            try
            {
                var child = tuple.Item1;
                var item = tuple.Item2;

                if (item == null && child.ItemId.HasValue)
                {
                    item = _libraryManager.GetItemById(child.ItemId.Value);
                }
                if (item == null && !string.IsNullOrEmpty(child.Path))
                {
                    item = _libraryManager.FindByPath(child.Path, false);
                }

                if (item == null)
                {
                    continue;
                }

                var size = item.Size ?? GetFileSizeSafe(item.Path);
                if (size.HasValue)
                {
                    totalSizeBytes += size.Value;
                }

                MediaStream? videoStream = null;
                MediaStream? audioStream = null;
                try
                {
                    var mediaStreams = GetMediaStreamsSafe(item);
                    if (mediaStreams != null)
                    {
                        videoStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);
                        audioStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Audio);
                    }
                }
                catch
                {
                    // Media stream probing failed gracefully
                }

                bool canDeleteItem = userCanDelete;

                itemsList.Add(new TriageItemDto
                {
                    ItemId = item.Id,
                    PlaylistItemId = GetLinkedChildEntryIdSafe(child) ?? string.Empty,
                    Name = item.Name,
                    Path = item.Path,
                    Bitrate = item.TotalBitrate,
                    FormattedBitrate = item.TotalBitrate.HasValue ? FormatBitrate(item.TotalBitrate.Value) : null,
                    Width = item.Width,
                    Height = item.Height,
                    Resolution = FormatResolution(item.Width, item.Height),
                    VideoCodec = videoStream?.Codec?.ToUpperInvariant(),
                    AudioCodec = audioStream?.Codec?.ToUpperInvariant(),
                    Container = item.Container?.ToUpperInvariant(),
                    SizeBytes = size,
                    FormattedSize = size.HasValue ? FormatBytes(size.Value) : "Unknown",
                    RunTimeTicks = item.RunTimeTicks,
                    FormattedDuration = FormatDuration(item.RunTimeTicks),
                    DateAdded = item.DateCreated,
                    FlagTimestamp = item.DateModified,
                    MediaType = GetMediaTypeStringSafe(item),
                    Overview = item.Overview,
                    PrimaryImageTag = item.HasImage(ImageType.Primary, 0) ? item.Id.ToString("N") : null,
                    CanDelete = canDeleteItem
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing individual playlist item in triage list");
            }
        }

        return new TriageListResponse
        {
            PlaylistId = targetPlaylist.Id,
            PlaylistName = targetPlaylist.Name,
            TotalItems = itemsList.Count,
            TotalSizeBytes = totalSizeBytes,
            FormattedTotalSize = FormatBytes(totalSizeBytes),
            Items = itemsList,
            UserCanDeleteFiles = userCanDelete
        };
    }

    /// <summary>
    /// Executes bulk resolution actions: remove from playlist or purge permanently from disk.
    /// </summary>
    [HttpPost("ResolveBatch")]
    [ProducesResponseType(typeof(ResolveBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResolveBatchResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResolveBatchResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResolveBatchResponse>> ResolveBatch([FromBody] ResolveBatchRequest request)
    {
        if (request == null || request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new ResolveBatchResponse
            {
                Success = false,
                Message = "No items provided for batch resolution."
            });
        }

        var action = request.Action?.Trim().ToLowerInvariant();
        if (action != "remove_from_playlist" && action != "delete_file_from_disk")
        {
            return BadRequest(new ResolveBatchResponse
            {
                Success = false,
                Message = "Invalid action. Supported actions: 'remove_from_playlist', 'delete_file_from_disk'."
            });
        }

        var callingUserId = ResolveCallingUserId(request.UserId);

        if (action == "delete_file_from_disk" && !CheckUserCanDelete(callingUserId))
        {
            _logger.LogWarning("User {UserId} attempted to delete files from disk without permission", callingUserId);
            return StatusCode(StatusCodes.Status403Forbidden, new ResolveBatchResponse
            {
                Success = false,
                Message = "Forbidden: User does not have permission to delete content from disk."
            });
        }

        var defaultName = Plugin.Instance?.Configuration.DefaultPlaylistName;
        if (string.IsNullOrWhiteSpace(defaultName))
        {
            defaultName = "Triage Queue";
        }

        var targetPlaylistName = !string.IsNullOrWhiteSpace(request.PlaylistName)
            ? request.PlaylistName.Trim()
            : defaultName;

        var existingPlaylists = callingUserId != Guid.Empty
            ? _playlistManager.GetPlaylists(callingUserId).OfType<Playlist>()
            : QueryAllPlaylistsFromLibrarySafe();

        var playlist = request.PlaylistId.HasValue && request.PlaylistId.Value != Guid.Empty
            ? existingPlaylists.FirstOrDefault(p => p.Id == request.PlaylistId.Value)
            : existingPlaylists.FirstOrDefault(p => string.Equals(p.Name, targetPlaylistName, StringComparison.OrdinalIgnoreCase));

        int processedCount = 0;
        long reclaimedBytes = 0;
        var errors = new List<string>();
        var entryIdsToRemove = new List<string>();

        var manageableItems = playlist != null ? GetPlaylistItemsSafe(playlist) : null;

        foreach (var target in request.Items)
        {
            try
            {
                var item = _libraryManager.GetItemById(target.ItemId);

                var entryId = target.PlaylistItemId;
                if (string.IsNullOrEmpty(entryId) && manageableItems != null)
                {
                    var match = manageableItems.FirstOrDefault(m =>
                        (m.Item2 != null && m.Item2.Id == target.ItemId) ||
                        (m.Item1.ItemId.HasValue && m.Item1.ItemId.Value == target.ItemId));
                    if (match != null)
                    {
                        entryId = GetLinkedChildEntryIdSafe(match.Item1);
                    }
                }

                if (!string.IsNullOrEmpty(entryId))
                {
                    entryIdsToRemove.Add(entryId);
                }

                if (action == "delete_file_from_disk")
                {
                    if (item == null)
                    {
                        errors.Add($"Item {target.ItemId} not found in library.");
                        continue;
                    }

                    var itemSize = item.Size ?? GetFileSizeSafe(item.Path) ?? 0;
                    var itemPath = item.Path;

                    _logger.LogWarning("Purging media item '{ItemName}' ({ItemId}) at path: {Path} by user {UserId}",
                        item.Name, item.Id, itemPath, callingUserId);

                    _libraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = true }, notifyParentItem: true);
                    reclaimedBytes += itemSize;
                    processedCount++;
                }
                else
                {
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item {ItemId} in batch resolution", target.ItemId);
                errors.Add($"Error on item {target.ItemId}: {ex.Message}");
            }
        }

        if (playlist != null)
        {
            var targetIds = new HashSet<Guid>(request.Items.Select(i => i.ItemId));
            var matchedTuples = (manageableItems ?? new List<Tuple<LinkedChild, BaseItem>>())
                .Where(m =>
                    (m.Item2 != null && targetIds.Contains(m.Item2.Id)) ||
                    (m.Item1.ItemId.HasValue && targetIds.Contains(m.Item1.ItemId.Value)))
                .ToList();

            if (matchedTuples.Count > 0)
            {
                try
                {
                    await RemoveItemsFromPlaylistInternalAsync(playlist, matchedTuples).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error removing items from playlist {PlaylistId}", playlist.Id);
                }
            }
        }

        var message = action == "delete_file_from_disk"
            ? $"Successfully purged {processedCount} items from disk, reclaiming {FormatBytes(reclaimedBytes)}."
            : $"Successfully removed {processedCount} items from playlist '{targetPlaylistName}'.";

        return Ok(new ResolveBatchResponse
        {
            Success = errors.Count == 0,
            Action = action,
            RequestedCount = request.Items.Count,
            ProcessedCount = processedCount,
            ReclaimedSizeBytes = reclaimedBytes,
            FormattedReclaimedSize = FormatBytes(reclaimedBytes),
            Errors = errors,
            Message = message
        });
    }

    /// <summary>
    /// Returns available user playlists merged with configured triage playlists for destination switching.
    /// </summary>
    [HttpGet("Playlists")]
    [ProducesResponseType(typeof(List<PlaylistSummaryDto>), StatusCodes.Status200OK)]
    public ActionResult<List<PlaylistSummaryDto>> GetPlaylists([FromQuery] Guid? userId = null)
    {
        SetNoCacheHeaders();
        try
        {
            return Ok(ExecuteWithRetry(() => BuildMergedPlaylistList(userId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve playlists. Returning fallback default playlists.");
            return Ok(GetFallbackPlaylists());
        }
    }

    /// <summary>
    /// Creates or registers a new playlist with custom icon, OSD pin status, and default status.
    /// </summary>
    [HttpPost("CreatePlaylist")]
    [ProducesResponseType(typeof(List<PlaylistSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PlaylistSummaryDto>>> CreatePlaylist([FromBody] CreatePlaylistRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Playlist name cannot be empty." });
        }

        var name = request.Name.Trim();
        var iconKey = string.IsNullOrWhiteSpace(request.IconKey) ? InferDefaultIconKey(name) : request.IconKey.Trim();
        var userId = ResolveCallingUserId(request.UserId);

        // Try creating in Jellyfin playlist manager as well
        try
        {
            var existing = userId != Guid.Empty
                ? _playlistManager.GetPlaylists(userId).OfType<Playlist>()
                : QueryAllPlaylistsFromLibrarySafe();

            if (!existing.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                await CreatePlaylistSafeAsync(name, userId, null).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Note: Jellyfin CreatePlaylist for empty playlist '{PlaylistName}' deferred until first item added.", name);
        }

        EnsurePlaylistRegisteredInConfig(name, iconKey, request.PinToOsd, request.SetAsDefault);

        return Ok(BuildMergedPlaylistList(request.UserId));
    }

    /// <summary>
    /// Updates playlist settings (icon, pin to OSD, set as default).
    /// </summary>
    [HttpPost("UpdatePlaylistSettings")]
    [ProducesResponseType(typeof(List<PlaylistSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<PlaylistSummaryDto>> UpdatePlaylistSettings([FromBody] UpdatePlaylistSettingsRequest request)
    {
        SetNoCacheHeaders();
        if (request == null || string.IsNullOrWhiteSpace(request.PlaylistName))
        {
            return BadRequest(new { message = "Playlist name is required." });
        }

        var callingUserId = ResolveCallingUserId(request.UserId);
        if (!CheckUserIsAdmin(callingUserId))
        {
            _logger.LogWarning("User {UserId} attempted to delete playlist {Playlist} without admin permission",
                callingUserId, request.PlaylistName);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden: Administrative privileges required to delete triage playlists." });
        }

        var name = request.PlaylistName.Trim();
        if (Plugin.Instance != null)
        {
            var config = Plugin.Instance.Configuration;
            config.PlaylistIconMappings ??= new List<PlaylistIconMapping>();
            config.QuickAccessPlaylists ??= new List<string>();

            var mapping = config.PlaylistIconMappings.FirstOrDefault(m =>
                string.Equals(m.PlaylistName, name, StringComparison.OrdinalIgnoreCase));

            if (mapping == null)
            {
                mapping = new PlaylistIconMapping
                {
                    PlaylistName = name,
                    IconKey = !string.IsNullOrWhiteSpace(request.IconKey) ? request.IconKey.Trim() : InferDefaultIconKey(name)
                };
                config.PlaylistIconMappings.Add(mapping);
            }
            else if (!string.IsNullOrWhiteSpace(request.IconKey))
            {
                mapping.IconKey = request.IconKey.Trim();
            }

            if (request.PinToOsd.HasValue)
            {
                var existingPin = config.QuickAccessPlaylists.FirstOrDefault(q =>
                    string.Equals(q, name, StringComparison.OrdinalIgnoreCase));
                if (request.PinToOsd.Value && existingPin == null)
                {
                    config.QuickAccessPlaylists.Add(name);
                }
                else if (!request.PinToOsd.Value && existingPin != null)
                {
                    config.QuickAccessPlaylists.RemoveAll(q => string.Equals(q, name, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (request.SetAsDefault == true)
            {
                config.DefaultPlaylistName = name;
                // Ensure default playlist is also pinned on OSD unless explicitly unpinned
                if (request.PinToOsd != false && !config.QuickAccessPlaylists.Any(q => string.Equals(q, name, StringComparison.OrdinalIgnoreCase)))
                {
                    config.QuickAccessPlaylists.Add(name);
                }
            }

            SafeUpdateConfiguration(config);
        }

        return Ok(ExecuteWithRetry(() => BuildMergedPlaylistList(request.UserId)));
    }

    /// <summary>
    /// Deletes/removes a playlist from triage configuration and removes empty playlist container if present.
    /// </summary>
    [HttpPost("DeletePlaylist")]
    [ProducesResponseType(typeof(List<PlaylistSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<PlaylistSummaryDto>> DeletePlaylist([FromBody] DeletePlaylistRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PlaylistName))
        {
            return BadRequest(new { message = "Playlist name is required." });
        }

        var callingUserId = ResolveCallingUserId(request.UserId);
        if (!CheckUserIsAdmin(callingUserId))
        {
            _logger.LogWarning("User {UserId} attempted to delete playlist {Playlist} without admin permission",
                callingUserId, request.PlaylistName);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden: Administrative privileges required to delete triage playlists." });
        }

        var name = request.PlaylistName.Trim();
        if (Plugin.Instance != null)
        {
            var config = Plugin.Instance.Configuration;
            config.PlaylistIconMappings?.RemoveAll(m => string.Equals(m.PlaylistName, name, StringComparison.OrdinalIgnoreCase));
            config.QuickAccessPlaylists?.RemoveAll(q => string.Equals(q, name, StringComparison.OrdinalIgnoreCase));

            if (string.Equals(config.DefaultPlaylistName, name, StringComparison.OrdinalIgnoreCase))
            {
                config.DefaultPlaylistName = config.PlaylistIconMappings?.FirstOrDefault()?.PlaylistName
                    ?? config.QuickAccessPlaylists?.FirstOrDefault()
                    ?? "Triage Queue";
            }

            SafeUpdateConfiguration(config);
        }

        // Also delete the playlist entity in Jellyfin if it exists (does NOT delete media files)
        try
        {
            var existing = callingUserId != Guid.Empty
                ? _playlistManager.GetPlaylists(callingUserId).OfType<Playlist>()
                : QueryAllPlaylistsFromLibrarySafe();

            var target = existing.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                _libraryManager.DeleteItem(target, new DeleteOptions { DeleteFileLocation = true }, notifyParentItem: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not remove Jellyfin playlist container '{PlaylistName}'", name);
        }

        return Ok(BuildMergedPlaylistList(request.UserId));
    }

    /// <summary>
    /// Purges all items within a playlist / queue from disk.
    /// </summary>
    [HttpPost("PurgeEntireQueue")]
    [ProducesResponseType(typeof(ResolveBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResolveBatchResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResolveBatchResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResolveBatchResponse>> PurgeEntireQueue([FromBody] PurgeQueueRequest request)
    {
        if (request == null || !request.ConfirmPurge)
        {
            return BadRequest(new ResolveBatchResponse
            {
                Success = false,
                Message = "Explicit confirmation 'ConfirmPurge=true' is required to permanently purge the entire queue."
            });
        }

        var callingUserId = ResolveCallingUserId(request.UserId);

        if (!CheckUserCanDelete(callingUserId))
        {
            _logger.LogWarning("User {UserId} attempted to purge queue {Playlist} without delete permission",
                callingUserId, request.PlaylistName);
            return StatusCode(StatusCodes.Status403Forbidden, new ResolveBatchResponse
            {
                Success = false,
                Message = "Forbidden: User does not have permission to delete content from disk."
            });
        }

        var targetPlaylistName = !string.IsNullOrWhiteSpace(request.PlaylistName)
            ? request.PlaylistName.Trim()
            : (Plugin.Instance?.Configuration.DefaultPlaylistName ?? "Delete Queue");

        var existingPlaylists = callingUserId != Guid.Empty
            ? _playlistManager.GetPlaylists(callingUserId).OfType<Playlist>()
            : QueryAllPlaylistsFromLibrarySafe();

        var playlist = request.PlaylistId.HasValue && request.PlaylistId.Value != Guid.Empty
            ? existingPlaylists.FirstOrDefault(p => p.Id == request.PlaylistId.Value)
            : existingPlaylists.FirstOrDefault(p => string.Equals(p.Name, targetPlaylistName, StringComparison.OrdinalIgnoreCase));

        if (playlist == null)
        {
            return Ok(new ResolveBatchResponse
            {
                Success = true,
                Action = "delete_entire_queue",
                RequestedCount = 0,
                ProcessedCount = 0,
                ReclaimedSizeBytes = 0,
                FormattedReclaimedSize = "0 B",
                Message = $"Queue '{targetPlaylistName}' is already empty."
            });
        }

        var manageableItems = GetPlaylistItemsSafe(playlist);
        int processedCount = 0;
        long reclaimedBytes = 0;
        var errors = new List<string>();
        var entryIdsToRemove = new List<string>();

        foreach (var tuple in manageableItems)
        {
            var child = tuple.Item1;
            var item = tuple.Item2;

            if (item == null && child.ItemId.HasValue) item = _libraryManager.GetItemById(child.ItemId.Value);
            if (item == null && !string.IsNullOrEmpty(child.Path)) item = _libraryManager.FindByPath(child.Path, false);

            var childId = GetLinkedChildEntryIdSafe(child);
            if (!string.IsNullOrEmpty(childId))
            {
                entryIdsToRemove.Add(childId);
            }

            if (item == null) continue;

            try
            {
                var itemSize = item.Size ?? GetFileSizeSafe(item.Path) ?? 0;
                _libraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = true }, notifyParentItem: true);
                reclaimedBytes += itemSize;
                processedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to delete {item.Name}: {ex.Message}");
            }
        }

        if (manageableItems.Count > 0)
        {
            try
            {
                await RemoveItemsFromPlaylistInternalAsync(playlist, manageableItems).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing entries from playlist {PlaylistId}", playlist.Id);
            }
        }

        return Ok(new ResolveBatchResponse
        {
            Success = errors.Count == 0,
            Action = "delete_entire_queue",
            RequestedCount = manageableItems.Count,
            ProcessedCount = processedCount,
            ReclaimedSizeBytes = reclaimedBytes,
            FormattedReclaimedSize = FormatBytes(reclaimedBytes),
            Errors = errors,
            Message = $"Purged entire queue '{targetPlaylistName}': {processedCount} items deleted, reclaiming {FormatBytes(reclaimedBytes)}."
        });
    }

    /// <summary>
    /// Assigns or updates a custom icon for a playlist.
    /// </summary>
    [HttpPost("AssignPlaylistIcon")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult AssignPlaylistIcon([FromBody] AssignIconRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PlaylistName))
        {
            return BadRequest(new { message = "Invalid playlist name" });
        }

        EnsurePlaylistRegisteredInConfig(request.PlaylistName.Trim(), request.IconKey, false, false);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Toggles or sets player controls swapping (play controls right, volume left).
    /// </summary>
    [HttpPost("ToggleOsdSwap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult ToggleOsdSwap([FromQuery] bool? swap = null)
    {
        if (Plugin.Instance != null)
        {
            var config = Plugin.Instance.Configuration;
            config.SwapOsdControls = swap.HasValue ? swap.Value : !config.SwapOsdControls;
            SafeUpdateConfiguration(config);
            return Ok(new { success = true, swapOsdControls = config.SwapOsdControls });
        }

        return Ok(new { success = false });
    }

    /// <summary>
    /// Updates player OSD and overlay preferences.
    /// </summary>
    [HttpPost("SaveOverlayPreferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult SaveOverlayPreferences([FromBody] OverlayPreferencesRequest request)
    {
        if (Plugin.Instance != null && request != null)
        {
            var config = Plugin.Instance.Configuration;
            if (request.SwapOsdControls.HasValue) config.SwapOsdControls = request.SwapOsdControls.Value;
            if (request.HideStopButton.HasValue) config.HideStopButton = request.HideStopButton.Value;
            if (request.HidePrevNextButtons.HasValue) config.HidePrevNextButtons = request.HidePrevNextButtons.Value;
            if (request.ShowFloatingQuickOverlay.HasValue) config.ShowFloatingQuickOverlay = request.ShowFloatingQuickOverlay.Value;
            if (!string.IsNullOrWhiteSpace(request.ConfiguredHotkey)) config.ConfiguredHotkey = request.ConfiguredHotkey.Trim();
            if (request.ActiveThemeCss != null) config.ActiveThemeCss = request.ActiveThemeCss;

            SafeUpdateConfiguration(config);
            return Ok(config);
        }

        return Ok(new { success = false });
    }

    /// <summary>
    /// Updates the configured hotkey.
    /// </summary>
    [HttpPost("SetHotkey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult SetHotkey([FromQuery] string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return BadRequest(new { message = "Hotkey string cannot be empty." });
        }

        if (Plugin.Instance != null)
        {
            var config = Plugin.Instance.Configuration;
            config.ConfiguredHotkey = hotkey.Trim();
            SafeUpdateConfiguration(config);
            return Ok(new { success = true, configuredHotkey = config.ConfiguredHotkey });
        }

        return Ok(new { success = false });
    }

    /// <summary>
    /// Returns current plugin configuration.
    /// </summary>
    [HttpGet("Configuration")]
    [ProducesResponseType(typeof(PluginConfiguration), StatusCodes.Status200OK)]
    public ActionResult<PluginConfiguration> GetConfiguration()
    {
        SetNoCacheHeaders();
        return Ok(Plugin.Instance?.Configuration ?? new PluginConfiguration());
    }

    /// <summary>
    /// Updates plugin configuration (admin only).
    /// </summary>
    [HttpPost("Configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult UpdateConfiguration([FromBody] PluginConfiguration newConfig)
    {
        var callingUserId = ResolveCallingUserId(null);
        if (!CheckUserIsAdmin(callingUserId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden: Administrative privileges required." });
        }

        if (Plugin.Instance != null && newConfig != null)
        {
            SafeUpdateConfiguration(newConfig);
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Serves client-side injection script for Jellyfin Web / Desktop.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("ClientScript")]
    [Produces("application/javascript")]
    public ActionResult GetClientScript()
    {
        SetNoCacheHeaders();
        var assembly = typeof(Plugin).Assembly;
        var resourceName = typeof(Plugin).Namespace + ".Web.triageClient.js";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return Content("// triageClient.js not found in embedded resources", "application/javascript");
        }

        using var reader = new StreamReader(stream);
        var script = reader.ReadToEnd();
        return Content(script, "application/javascript");
    }

        private void SetNoCacheHeaders()
    {
        try
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
        }
        catch
        {
            // Ignore header modification errors
        }
    }

    private void SafeUpdateConfiguration(PluginConfiguration config)
    {
        try
        {
            Plugin.Instance?.UpdateConfiguration(config);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist plugin configuration XML to disk (kept in memory).");
        }
    }

    private bool IsLibraryName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        try
        {
            var virtualFolders = _libraryManager.GetVirtualFolders();
            return virtualFolders.Any(vf => string.Equals(vf.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, int delayMs = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (i < maxRetries - 1 && IsTransientDatabaseException(ex))
            {
                _logger.LogWarning("Transient database lock encountered in TriageController async (attempt {Attempt}/{MaxRetries}): {Message}. Retrying...", i + 1, maxRetries, ex.Message);
                await Task.Delay(delayMs * (i + 1)).ConfigureAwait(false);
            }
        }
        return await action().ConfigureAwait(false);
    }

    private T ExecuteWithRetry<T>(Func<T> action, int maxRetries = 3, int delayMs = 60)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (i < maxRetries - 1 && IsTransientDatabaseException(ex))
            {
                _logger.LogWarning("Transient database lock encountered in TriageController (attempt {Attempt}/{MaxRetries}): {Message}. Retrying...", i + 1, maxRetries, ex.Message);
                Thread.Sleep(delayMs * (i + 1));
            }
        }
        return action();
    }

    private static bool IsTransientDatabaseException(Exception ex)
    {
        var msg = ex.ToString();
        return msg.Contains("SQLite Error 10", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("SQLite Error 5", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("SQLite Error 14", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("unable to open database file", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("database is locked", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("disk I/O error", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("busy", StringComparison.OrdinalIgnoreCase);
    }

    private static List<PlaylistSummaryDto> GetFallbackPlaylists()
    {
        return new List<PlaylistSummaryDto>
        {
            new() { Id = Guid.Empty, Name = "Triage Queue", ItemCount = 0, IconKey = "bookmark", IsDefault = true, IsPinned = true },
            new() { Id = Guid.Empty, Name = "Delete Queue", ItemCount = 0, IconKey = "trash", IsDefault = false, IsPinned = true },
            new() { Id = Guid.Empty, Name = "Watch Later", ItemCount = 0, IconKey = "clock", IsDefault = false, IsPinned = true }
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Robustly removes entries from a Playlist by updating LinkedChildren directly,
    /// committing to the database repository, updating physical file on disk (if file-backed),
    /// and calling RemoveItemFromPlaylistAsync as a fallback.
    /// </summary>
    private async Task RemoveItemsFromPlaylistInternalAsync(Playlist playlist, List<Tuple<LinkedChild, BaseItem>> matchesToRemove)
    {
        if (matchesToRemove.Count == 0)
        {
            return;
        }

        // 1. Directly exclude the matched LinkedChildren from playlist.LinkedChildren
        var matchChildren = new HashSet<LinkedChild>(matchesToRemove.Select(m => m.Item1));
        var currentLinked = playlist.LinkedChildren ?? Array.Empty<LinkedChild>();
        playlist.LinkedChildren = currentLinked.Where(c => !matchChildren.Contains(c)).ToArray();

        // 2. Commit update to database repository
        await playlist.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

        // 3. If file-based playlist (.xml / .m3u), commit changes to disk
        if (playlist.IsFile)
        {
            try
            {
                _playlistManager.SavePlaylistFile(playlist);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update physical playlist file for {PlaylistName}", playlist.Name);
            }
        }

        // 4. Also call RemoveItemFromPlaylistAsync with all possible entry ID representations as supplementary measure
        var allEntryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in matchesToRemove)
        {
            var entryId = GetLinkedChildEntryIdSafe(m.Item1);
            if (!string.IsNullOrEmpty(entryId))
            {
                allEntryIds.Add(entryId);
            }

            if (m.Item1.ItemId.HasValue)
            {
                allEntryIds.Add(m.Item1.ItemId.Value.ToString("N", CultureInfo.InvariantCulture));
                allEntryIds.Add(m.Item1.ItemId.Value.ToString("D", CultureInfo.InvariantCulture));
            }

            if (m.Item2 != null)
            {
                allEntryIds.Add(m.Item2.Id.ToString("N", CultureInfo.InvariantCulture));
                allEntryIds.Add(m.Item2.Id.ToString("D", CultureInfo.InvariantCulture));
            }
        }

        if (allEntryIds.Count > 0)
        {
            try
            {
                await _playlistManager.RemoveItemFromPlaylistAsync(playlist.Id.ToString(), allEntryIds).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "RemoveItemFromPlaylistAsync supplementary call for {PlaylistId}", playlist.Id);
            }
        }
    }

    private List<PlaylistSummaryDto> BuildMergedPlaylistList(Guid? explicitUserId)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();

        // 1. Auto-sanitize legacy "Vidz" or library folder collision from config
        if (string.IsNullOrWhiteSpace(config.DefaultPlaylistName) ||
            string.Equals(config.DefaultPlaylistName, "Vidz", StringComparison.OrdinalIgnoreCase) ||
            IsLibraryName(config.DefaultPlaylistName))
        {
            config.DefaultPlaylistName = "Triage Queue";
            Plugin.Instance?.SaveConfiguration();
        }

        if (config.QuickAccessPlaylists != null)
        {
            var removed = config.QuickAccessPlaylists.RemoveAll(q =>
                string.Equals(q, "Vidz", StringComparison.OrdinalIgnoreCase) || IsLibraryName(q));
            // Deduplicate
            var unique = config.QuickAccessPlaylists.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (unique.Count != config.QuickAccessPlaylists.Count || removed > 0)
            {
                config.QuickAccessPlaylists = unique;
                if (config.QuickAccessPlaylists.Count == 0)
                {
                    config.QuickAccessPlaylists.Add("Triage Queue");
                    config.QuickAccessPlaylists.Add("Delete Queue");
                }
                Plugin.Instance?.SaveConfiguration();
            }
        }

        var iconMappings = config.PlaylistIconMappings ?? new List<PlaylistIconMapping>();
        var quickAccess = (config.QuickAccessPlaylists ?? new List<string>())
            .Where(q => !string.IsNullOrWhiteSpace(q) && !string.Equals(q, "Vidz", StringComparison.OrdinalIgnoreCase) && !IsLibraryName(q))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var defaultName = !string.IsNullOrWhiteSpace(config.DefaultPlaylistName) ? config.DefaultPlaylistName : "Triage Queue";
        if (string.Equals(defaultName, "Vidz", StringComparison.OrdinalIgnoreCase) || IsLibraryName(defaultName))
        {
            defaultName = "Triage Queue";
        }

        var callingUserId = ResolveCallingUserId(explicitUserId);
        List<Playlist> playlists = new();
        try
        {
            if (callingUserId != Guid.Empty)
            {
                playlists = _playlistManager.GetPlaylists(callingUserId).OfType<Playlist>().ToList();
            }

            if (playlists.Count == 0)
            {
                playlists = QueryAllPlaylistsFromLibrarySafe();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve playlists from Jellyfin database");
        }

        var byName = new Dictionary<string, PlaylistSummaryDto>(StringComparer.OrdinalIgnoreCase);

        // 1. Add all real Jellyfin playlists
        foreach (var p in playlists)
        {
            if (string.IsNullOrWhiteSpace(p.Name) ||
                string.Equals(p.Name, "Vidz", StringComparison.OrdinalIgnoreCase) ||
                IsLibraryName(p.Name))
            {
                continue;
            }

            var customIcon = iconMappings.FirstOrDefault(m =>
                string.Equals(m.PlaylistName, p.Name, StringComparison.OrdinalIgnoreCase))?.IconKey;

            if (string.IsNullOrEmpty(customIcon))
            {
                customIcon = InferDefaultIconKey(p.Name);
            }

            var manageableList = GetPlaylistItemsSafe(p);

            var itemGuids = manageableList
                .Select(m => m.Item2?.Id ?? m.Item1.ItemId ?? Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            byName[p.Name] = new PlaylistSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                ItemCount = manageableList.Count,
                ItemIds = itemGuids,
                IconKey = customIcon,
                IsDefault = string.Equals(p.Name, defaultName, StringComparison.OrdinalIgnoreCase),
                IsPinned = quickAccess.Any(q => string.Equals(q, p.Name, StringComparison.OrdinalIgnoreCase))
            };
        }

        // 2. Merge configured icon mappings / registered playlists so empty/new playlists always show
        foreach (var m in iconMappings)
        {
            if (string.IsNullOrWhiteSpace(m.PlaylistName)) continue;
            var name = m.PlaylistName.Trim();
            if (string.Equals(name, "Vidz", StringComparison.OrdinalIgnoreCase) || IsLibraryName(name))
            {
                continue;
            }

            if (!byName.ContainsKey(name))
            {
                byName[name] = new PlaylistSummaryDto
                {
                    Id = Guid.Empty,
                    Name = name,
                    ItemCount = 0,
                    IconKey = !string.IsNullOrWhiteSpace(m.IconKey) ? m.IconKey : InferDefaultIconKey(name),
                    IsDefault = string.Equals(name, defaultName, StringComparison.OrdinalIgnoreCase),
                    IsPinned = quickAccess.Any(q => string.Equals(q, name, StringComparison.OrdinalIgnoreCase))
                };
            }
        }

        // 3. Merge quickAccessPlaylists
        foreach (var q in quickAccess)
        {
            if (string.IsNullOrWhiteSpace(q)) continue;
            var name = q.Trim();
            if (string.Equals(name, "Vidz", StringComparison.OrdinalIgnoreCase) || IsLibraryName(name))
            {
                continue;
            }

            if (!byName.ContainsKey(name))
            {
                byName[name] = new PlaylistSummaryDto
                {
                    Id = Guid.Empty,
                    Name = name,
                    ItemCount = 0,
                    IconKey = InferDefaultIconKey(name),
                    IsDefault = string.Equals(name, defaultName, StringComparison.OrdinalIgnoreCase),
                    IsPinned = true
                };
            }
        }

        // 4. Ensure default playlist is present
        if (!byName.ContainsKey(defaultName))
        {
            byName[defaultName] = new PlaylistSummaryDto
            {
                Id = Guid.Empty,
                Name = defaultName,
                ItemCount = 0,
                IconKey = InferDefaultIconKey(defaultName),
                IsDefault = true,
                IsPinned = true
            };
        }

        return byName.Values
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.IsPinned)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void EnsurePlaylistRegisteredInConfig(string name, string? iconKey, bool pinToOsd, bool setAsDefault)
    {
        if (Plugin.Instance == null || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var config = Plugin.Instance.Configuration;
        config.PlaylistIconMappings ??= new List<PlaylistIconMapping>();
        config.QuickAccessPlaylists ??= new List<string>();

        var existing = config.PlaylistIconMappings.FirstOrDefault(m =>
            string.Equals(m.PlaylistName, name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            config.PlaylistIconMappings.Add(new PlaylistIconMapping
            {
                PlaylistName = name,
                IconKey = !string.IsNullOrWhiteSpace(iconKey) ? iconKey : InferDefaultIconKey(name)
            });
        }
        else if (!string.IsNullOrWhiteSpace(iconKey))
        {
            existing.IconKey = iconKey;
        }

        if (pinToOsd && !config.QuickAccessPlaylists.Any(q => string.Equals(q, name, StringComparison.OrdinalIgnoreCase)))
        {
            config.QuickAccessPlaylists.Add(name);
        }

        if (setAsDefault)
        {
            config.DefaultPlaylistName = name;
        }

        config.PlaylistIconMappings = config.PlaylistIconMappings
            .GroupBy(m => m.PlaylistName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();
        config.QuickAccessPlaylists = config.QuickAccessPlaylists
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        SafeUpdateConfiguration(config);
    }

    private static string InferDefaultIconKey(string name)
    {
        var lower = (name ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("delete") || lower.Contains("trash") || lower.Contains("purge")) return "trash";
        if (lower.Contains("star") || lower.Contains("fav")) return "star";
        if (lower.Contains("prune") || lower.Contains("cut")) return "scissors";
        if (lower.Contains("clock") || lower.Contains("later")) return "clock";
        if (lower.Contains("archive") || lower.Contains("box")) return "archive";
        if (lower.Contains("fire") || lower.Contains("hot")) return "fire";
        if (lower.Contains("heart") || lower.Contains("love")) return "heart";
        if (lower.Contains("film") || lower.Contains("movie") || lower.Contains("vid")) return "film";
        if (lower.Contains("folder")) return "folder";
        return "bookmark";
    }

    private bool CheckUserCanDelete(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return false;
        }

        return user.HasPermission(PermissionKind.IsAdministrator) ||
               user.HasPermission(PermissionKind.EnableContentDeletion);
    }

    private bool CheckUserIsAdmin(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var user = _userManager.GetUserById(userId);
        return user != null && user.HasPermission(PermissionKind.IsAdministrator);
    }

    private Guid ResolveCallingUserId(Guid? explicitUserId)
    {
        var claim = User?.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User?.FindFirst("UserId")
                 ?? User?.FindFirst("Jellyfin-UserId");

        Guid authenticatedUserId = Guid.Empty;
        if (claim != null && Guid.TryParse(claim.Value, out var parsedClaimId))
        {
            authenticatedUserId = parsedClaimId;
        }

        if (explicitUserId.HasValue && explicitUserId.Value != Guid.Empty)
        {
            if (authenticatedUserId == explicitUserId.Value || CheckUserIsAdmin(authenticatedUserId))
            {
                return explicitUserId.Value;
            }

            _logger.LogWarning("Caller {AuthUser} attempted to act on behalf of user {TargetUser} without admin rights",
                authenticatedUserId, explicitUserId.Value);
        }

        if (authenticatedUserId != Guid.Empty)
        {
            return authenticatedUserId;
        }

        return Guid.Empty;
    }

    private SessionInfo? FindActivePlaybackSession(Guid? userId, string? deviceId, string? sessionId)
    {
        var allSessions = _sessionManager.Sessions.ToList();

        // 1. By sessionId (scoped to userId if known)
        if (!string.IsNullOrEmpty(sessionId))
        {
            var match = allSessions.FirstOrDefault(s => string.Equals(s.Id, sessionId, StringComparison.OrdinalIgnoreCase));
            if (match != null && (match.NowPlayingItem != null || match.FullNowPlayingItem != null))
            {
                if (!userId.HasValue || userId.Value == Guid.Empty || match.UserId == userId.Value)
                {
                    return match;
                }
            }
        }

        // 2. By deviceId + playback active (scoped to userId if known)
        if (!string.IsNullOrEmpty(deviceId))
        {
            var match = allSessions
                .Where(s => string.Equals(s.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase) &&
                            (!userId.HasValue || userId.Value == Guid.Empty || s.UserId == userId.Value) &&
                            (s.NowPlayingItem != null || s.FullNowPlayingItem != null))
                .OrderByDescending(s => s.LastPlaybackCheckIn)
                .ThenByDescending(s => s.LastActivityDate)
                .FirstOrDefault();

            if (match != null)
            {
                return match;
            }
        }

        // 3. By userId + playback active
        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            var userMatch = allSessions
                .Where(s => s.UserId == userId.Value && (s.NowPlayingItem != null || s.FullNowPlayingItem != null))
                .OrderByDescending(s => s.LastPlaybackCheckIn)
                .ThenByDescending(s => s.LastActivityDate)
                .FirstOrDefault();

            if (userMatch != null)
            {
                return userMatch;
            }
        }

        // 4. Fallback: match by deviceId even if playback checkin was slightly delayed
        if (!string.IsNullOrEmpty(deviceId))
        {
            var devMatch = allSessions.FirstOrDefault(s =>
                string.Equals(s.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase) &&
                (!userId.HasValue || userId.Value == Guid.Empty || s.UserId == userId.Value));
            if (devMatch != null)
            {
                return devMatch;
            }
        }

        // 5. Fallback: match by userId if single session
        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            var userSessions = allSessions.Where(s => s.UserId == userId.Value).ToList();
            if (userSessions.Count > 0)
            {
                return userSessions.OrderByDescending(s => s.LastActivityDate).FirstOrDefault();
            }
        }

        return null;
    }

    private static string FormatResolution(int? width, int? height)
    {
        if (!width.HasValue && !height.HasValue)
        {
            return "Unknown";
        }

        var w = width ?? 0;
        var h = height ?? 0;

        if (w >= 3800 || h >= 2100) return "4K UHD";
        if (w >= 2500 || h >= 1400) return "1440p QHD";
        if (w >= 1900 || h >= 1000) return "1080p FHD";
        if (w >= 1200 || h >= 700) return "720p HD";
        if (h >= 480) return "480p SD";

        return $"{w}x{h}";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int i = 0;
        double dblSByte = bytes;
        while (Math.Round(dblSByte / 1024) >= 1 && i < suffixes.Length - 1)
        {
            dblSByte /= 1024;
            i++;
        }

        return $"{dblSByte:0.##} {suffixes[i]}";
    }

    private static string FormatBitrate(long bitrateBps)
    {
        if (bitrateBps <= 0) return "Unknown";
        if (bitrateBps >= 1_000_000)
        {
            return $"{bitrateBps / 1_000_000.0:0.1} Mbps";
        }

        return $"{bitrateBps / 1_000.0:0} kbps";
    }

    private static string FormatDuration(long? runTimeTicks)
    {
        if (!runTimeTicks.HasValue || runTimeTicks.Value <= 0)
        {
            return "--:--";
        }

        var ts = TimeSpan.FromTicks(runTimeTicks.Value);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private static long? GetFileSizeSafe(string? path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
        }
        catch
        {
            // Ignore IO exceptions
        }

        return null;
    }

    /// <summary>
    /// Triggers an immediate media library scan to pick up newly added media files.
    /// </summary>
    [HttpPost("ScanLibrary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult ScanLibrary()
    {
        var callingUserId = ResolveCallingUserId(null);
        if (!CheckUserIsAdmin(callingUserId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden: Administrative privileges required." });
        }

        _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
        return Ok(new { success = true, message = "Library scan triggered." });
    }

    private List<Tuple<LinkedChild, BaseItem>> GetPlaylistItemsSafe(Playlist? playlist)
    {
        var results = new List<Tuple<LinkedChild, BaseItem>>();
        if (playlist == null)
        {
            return results;
        }

        try
        {
            // Invoke GetManageableItems via reflection so return type differences (IEnumerable vs IReadOnlyList) between Jellyfin 10.9 and 12.x do not cause MissingMethodException
            var getManageableMethod = playlist.GetType().GetMethod("GetManageableItems", Type.EmptyTypes);
            if (getManageableMethod != null)
            {
                if (getManageableMethod.Invoke(playlist, null) is IEnumerable<Tuple<LinkedChild, BaseItem>> reflectedItems)
                {
                    results.AddRange(reflectedItems.Where(t => t != null));
                    if (results.Count > 0)
                    {
                        return results;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Reflection call to GetManageableItems fell back to LinkedChildren for playlist '{PlaylistName}'", playlist.Name);
        }

        try
        {
            var children = playlist.LinkedChildren;
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child == null) continue;
                    BaseItem? resolved = null;
                    if (child.ItemId.HasValue && child.ItemId.Value != Guid.Empty)
                    {
                        try { resolved = _libraryManager.GetItemById(child.ItemId.Value); } catch { }
                    }
                    if (resolved == null && !string.IsNullOrEmpty(child.Path))
                    {
                        try { resolved = _libraryManager.FindByPath(child.Path, false); } catch { }
                    }
                    if (resolved != null)
                    {
                        results.Add(new Tuple<LinkedChild, BaseItem>(child, resolved));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve LinkedChildren for playlist '{PlaylistName}'", playlist.Name);
        }

        return results;
    }

    private List<Playlist> QueryAllPlaylistsFromLibrarySafe()
    {
        try
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Playlist },
                Recursive = true
            };

            // Invoke GetItemList via reflection so List<BaseItem> (10.9) vs IReadOnlyList<BaseItem> (12.x) return type difference does not throw MissingMethodException
            var getItemListMethod = _libraryManager.GetType().GetMethod("GetItemList", new[] { typeof(InternalItemsQuery) })
                                 ?? typeof(ILibraryManager).GetMethod("GetItemList", new[] { typeof(InternalItemsQuery) });
            if (getItemListMethod != null)
            {
                if (getItemListMethod.Invoke(_libraryManager, new object[] { query }) is IEnumerable<BaseItem> items)
                {
                    return items.OfType<Playlist>().ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query playlists from LibraryManager");
        }

        return new List<Playlist>();
    }

    private static string? GetLinkedChildEntryIdSafe(LinkedChild? child)
    {
        if (child == null)
        {
            return null;
        }

        try
        {
            var type = child.GetType();
            var idProp = type.GetProperty("Id") ?? type.GetProperty("LibraryItemId");
            if (idProp != null)
            {
                var val = idProp.GetValue(child) as string;
                if (!string.IsNullOrEmpty(val))
                {
                    return val;
                }
            }
        }
        catch
        {
            // Ignore reflection error
        }

        if (child.ItemId.HasValue && child.ItemId.Value != Guid.Empty)
        {
            return child.ItemId.Value.ToString("N", CultureInfo.InvariantCulture);
        }

        return null;
    }

    private async Task AddItemToPlaylistSafeAsync(Guid playlistId, Guid[] itemIds, Guid userId)
    {
        var methods = _playlistManager.GetType().GetMethods()
            .Concat(typeof(IPlaylistManager).GetMethods())
            .Where(m => string.Equals(m.Name, "AddItemToPlaylistAsync", StringComparison.Ordinal))
            .ToList();

        // Jellyfin 12.x: AddItemToPlaylistAsync(Guid playlistId, IReadOnlyCollection<Guid> itemIds, int? position, Guid userId)
        var fourParam = methods.FirstOrDefault(m => m.GetParameters().Length == 4);
        if (fourParam != null)
        {
            if (fourParam.Invoke(_playlistManager, new object?[] { playlistId, itemIds, null, userId }) is Task t4)
            {
                await t4.ConfigureAwait(false);
                return;
            }
        }

        // Jellyfin 10.9: AddItemToPlaylistAsync(Guid playlistId, IReadOnlyCollection<Guid> itemIds, Guid userId)
        var threeParam = methods.FirstOrDefault(m => m.GetParameters().Length == 3);
        if (threeParam != null)
        {
            if (threeParam.Invoke(_playlistManager, new object?[] { playlistId, itemIds, userId }) is Task t3)
            {
                await t3.ConfigureAwait(false);
                return;
            }
        }
    }

    private async Task<PlaylistCreationResult?> CreatePlaylistSafeAsync(string name, Guid userId, Guid[]? itemIds)
    {
        var req = new PlaylistCreationRequest
        {
            Name = name,
            UserId = userId
        };

        // Set ItemIdList and Public via reflection to avoid bool vs bool? and Guid[] vs IReadOnlyList<Guid> binary mismatches across 10.9 and 12.x
        var reqType = req.GetType();
        try
        {
            var itemListProp = reqType.GetProperty("ItemIdList");
            if (itemListProp != null && itemIds != null && itemIds.Length > 0)
            {
                itemListProp.SetValue(req, itemIds);
            }
        }
        catch { }

        try
        {
            var publicProp = reqType.GetProperty("Public");
            if (publicProp != null)
            {
                publicProp.SetValue(req, true);
            }
        }
        catch { }

        return await _playlistManager.CreatePlaylist(req).ConfigureAwait(false);
    }

    private static IEnumerable<MediaStream>? GetMediaStreamsSafe(BaseItem item)
    {
        try
        {
            var method = item.GetType().GetMethod("GetMediaStreams", Type.EmptyTypes);
            if (method != null)
            {
                return method.Invoke(item, null) as IEnumerable<MediaStream>;
            }
        }
        catch
        {
            // Ignore reflection error
        }

        return null;
    }

    private static string GetMediaTypeStringSafe(BaseItem item)
    {
        try
        {
            var prop = item.GetType().GetProperty("MediaType");
            var val = prop?.GetValue(item);
            if (val != null)
            {
                return val.ToString() ?? "Video";
            }
        }
        catch
        {
            // Ignore reflection error
        }

        return "Video";
    }

    #endregion
}
