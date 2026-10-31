// ==UserScript==
// @name         Jellyfin Cnidocil
// @namespace    https://github.com/findawebguy/jellyfin-plugin-cnidocil
// @version      1.2.1.0
// @description  Cnidocil (jellyfin-plugin-cnidocil) - Rapid OSD triage queue manager, pruner, and batch pruner for Jellyfin libraries.
// @author       findawebguy
// @homepageURL  https://github.com/findawebguy/jellyfin-plugin-cnidocil
// @supportURL   https://github.com/findawebguy/jellyfin-plugin-cnidocil/issues
// @match        *://*/*
// @grant        none
// @run-at       document-idle
// ==/UserScript==

/**
 * Cnidocil (jellyfin-plugin-cnidocil) - Global Client & Playbar Extension
 *
 * Updates:
 * - Queue Toggle: Clicking a queue adds the active media if absent, or removes it if already present.
 * - Library Filters: Adds Triage Queues directly into "My Media" filter menus & quick filter chips.
 * - Removed global floating overlay dock (clean, non-intrusive UI).
 * - Simplistic, premium design matching Jellyfin's native dark theme (no gaudy neon glows).
 * - Automatic playbar idle visibility transitions (.videoOsdBottom).
 */

(function () {
    'use strict';

    function isJellyfinEnvironment() {
        return !!(
            window.ApiClient ||
            window.playbackManager ||
            document.querySelector('meta[name="application-name"][content*="Jellyfin"]') ||
            document.querySelector('#videoOsdPage, .videoOsdBottom, .skinHeader, .mainDrawer')
        );
    }

    if (!isJellyfinEnvironment()) {
        return;
    }

    // Chromium 129+ ScrollOptions.behavior strict validation safeguard (scoped exclusively to confirmed Jellyfin context)
    (function applyScrollPolyfill() {
        function s(a) {
            try {
                if (a && 1 === a.length && a[0] && typeof a[0] === 'object' && a[0] !== null) {
                    var o = a[0];
                    if (o.behavior === null || o.behavior === void 0 || (o.behavior !== 'auto' && o.behavior !== 'smooth' && o.behavior !== 'instant')) {
                        var c = Object.assign({}, o);
                        delete c.behavior;
                        return [c];
                    }
                }
            } catch (err) {}
            return a;
        }
        if (typeof Element !== 'undefined' && Element.prototype.scrollTo) {
            var e = Element.prototype.scrollTo;
            Element.prototype.scrollTo = function () {
                try {
                    return e.apply(this, s(arguments));
                } catch (t) {}
            };
        }
        if (typeof window !== 'undefined' && window.scrollTo) {
            var w = window.scrollTo;
            window.scrollTo = function () {
                try {
                    return w.apply(this, s(arguments));
                } catch (t) {}
            };
        }
    })();

    if (window.PlaylistTriageInitialized) {
        if (window.PlaylistTriageRefresh) {
            window.PlaylistTriageRefresh();
        }
        return;
    }
    window.PlaylistTriageInitialized = true;

    var ICONS = {
        trash: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"/></svg>',
        bookmark: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M17 3H7c-1.1 0-2 .9-2 2v16l7-3 7 3V5c0-1.1-.9-2-2-2zm0 15l-5-2.18L7 18V5h10v13z"/></svg>',
        star: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/></svg>',
        scissors: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M9.64 7.64c.23-.5.36-1.05.36-1.64 0-2.21-1.79-4-4-4S2 3.79 2 6s1.79 4 4 4c.59 0 1.14-.13 1.64-.36L10 12l-2.36 2.36C7.14 14.13 6.59 14 6 14c-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4c0-.59-.13-1.14-.36-1.64L12 14l7 7h3v-1L9.64 7.64zM6 8c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm0 12c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm9.64-9.64L22 3.64V3h-3l-5 5 1.64 1.64z"/></svg>',
        fire: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M19.48 12.35c-1.57-4.08-7.16-4.3-5.81-10.23.1-.44-.37-.78-.7-.52-3.13 2.5-6.57 6.36-5.83 11.59.08.57-.4 1.07-.97 1.07-.48 0-.89-.34-.98-.82-.6-3.14.73-6.19 1.48-8.24.16-.45-.33-.88-.74-.63-4.14 2.52-6.52 7.73-5.18 12.87 1.42 5.48 6.58 9.56 12.3 9.56 7.18 0 13-5.82 13-13 0-2.61-1-5.01-2.58-6.66-.34-.36-.92-.12-.97.37-.2 1.83-1.03 3.44-2.03 4.62z"/></svg>',
        heart: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/></svg>',
        archive: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M20.54 5.23l-1.39-1.68C18.88 3.21 18.47 3 18 3H6c-.47 0-.88.21-1.16.55L3.46 5.23C3.17 5.57 3 6.02 3 6.5V19c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V6.5c0-.48-.17-.93-.46-1.27zM12 17.5L6.5 12H10v-2h4v2h3.5L12 17.5zM5.12 5l.81-1h12l.94 1H5.12z"/></svg>',
        clock: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z"/></svg>',
        film: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M18 4l2 4h-3l-2-4h-2l2 4h-3l-2-4H8l2 4H7L5 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V4h-4z"/></svg>',
        swap: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M6.99 11L3 15l3.99 4v-3H14v-2H6.99v-3zM21 9l-3.99-4v3H10v2h7.01v3L21 9z"/></svg>',
        settings: '<svg viewBox="0 0 24 24" style="width:18px;height:18px;fill:currentColor;"><path d="M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"/></svg>',
        plus: '<svg viewBox="0 0 24 24" style="width:19px;height:19px;fill:currentColor;"><path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>',
        check: '<svg viewBox="0 0 24 24" style="width:16px;height:16px;fill:currentColor;"><path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/></svg>'
    };

    var CONFIG = {
        defaultPlaylistName: 'Triage Queue',
        configuredHotkey: 'Ctrl+Shift+B',
        swapOsdControls: false,
        hideStopButton: false,
        hidePrevNextButtons: false,
        activeThemeCss: '',
        quickAccessPlaylists: ['Triage Queue', 'Delete Queue', 'Watch Later'],
        playlistIconMappings: [
            { playlistName: 'Triage Queue', iconKey: 'bookmark' },
            { playlistName: 'Delete Queue', iconKey: 'trash' },
            { playlistName: 'Watch Later', iconKey: 'clock' },
            { playlistName: 'Favorites', iconKey: 'star' }
        ],
        toastDurationMs: 2500
    };

    var cachedPlaylists = [];
    var lastPlaylistFetchTime = 0;
    var hasPluginBackend = null;
    var activeQueueFilters = [];
    var lastKnownPlayingItem = null;

    // Global listener for Jellyfin playback events across all browsers
    try {
        var onPlaybackActive = function (e) {
            try {
                var it = (e && e.detail && e.detail.item) || (e && e.item);
                if (!it && window.playbackManager && typeof window.playbackManager.currentItem === 'function') {
                    it = window.playbackManager.currentItem();
                }
                if (it) {
                    var prevId = lastKnownPlayingItem ? normalizeGuid(lastKnownPlayingItem.Id || lastKnownPlayingItem.id) : '';
                    var nextId = normalizeGuid(it.Id || it.id);
                    lastKnownPlayingItem = it;
                    if (nextId && nextId !== prevId) {
                        updatePlaybarButtons();
                    }
                }
            } catch (err) {}
        };
        document.addEventListener('playbackstart', function (e) {
            onPlaybackActive(e);
            updatePlaybarButtons();
        }, true);
        document.addEventListener('playbackprogress', onPlaybackActive, true);
        document.addEventListener('timeupdate', onPlaybackActive, true);
        document.addEventListener('playbackstop', function () {
            setTimeout(function () {
                lastKnownPlayingItem = null;
                updatePlaybarButtons();
            }, 1000);
        }, true);
    } catch (e) {}

    function prop(obj, key) {
        if (!obj) return undefined;
        if (obj[key] !== undefined) return obj[key];
        var pascal = key.charAt(0).toUpperCase() + key.slice(1);
        return obj[pascal];
    }

    function escapeHtml(str) {
        if (str === null || str === undefined) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function getApiUrl(subpath) {
        if (typeof ApiClient !== 'undefined' && ApiClient.getUrl) {
            return ApiClient.getUrl('Plugins/PlaylistTriage/' + subpath);
        }
        return '/Plugins/PlaylistTriage/' + subpath;
    }

    function getCurrentUserIdSafe() {
        if (typeof ApiClient !== 'undefined' && ApiClient.getCurrentUserId) {
            return ApiClient.getCurrentUserId() || '';
        }
        return '';
    }

    function applyThemeCss(cssString) {
        var tag = document.getElementById('pt-active-theme-css');
        if (!cssString) {
            if (tag && tag.parentNode) tag.parentNode.removeChild(tag);
            return;
        }
        if (!tag) {
            tag = document.createElement('style');
            tag.id = 'pt-active-theme-css';
            document.head.appendChild(tag);
        }
        tag.textContent = cssString;
    }

    function ensureGlobalStyleTag() {
        var tag = document.getElementById('pt-global-playbar-style');
        if (!tag) {
            tag = document.createElement('style');
            tag.id = 'pt-global-playbar-style';
            document.head.appendChild(tag);
        }

        tag.textContent = [
            '/* Triage Container Capsule */',
            '#pt-playbar-capsule {',
            '    display: inline-flex !important; align-items: center !important; gap: 3px !important;',
            '    background: rgba(24, 24, 27, 0.72) !important;',
            '    border: 1px solid rgba(255, 255, 255, 0.1) !important;',
            '    border-radius: 999px !important; padding: 2px 5px !important;',
            '    backdrop-filter: blur(8px) !important; -webkit-backdrop-filter: blur(8px) !important;',
            '    margin: 0 4px !important; vertical-align: middle !important;',
            '    box-sizing: border-box !important; height: 38px !important;',
            '    transition: background 0.18s ease, border-color 0.18s ease !important;',
            '}',
            '#pt-playbar-capsule:hover {',
            '    background: rgba(24, 24, 27, 0.92) !important; border-color: rgba(255, 255, 255, 0.22) !important;',
            '}',
            '/* Native Button Theme: .paper-icon-button-light.emby-button */',
            '.pt-playbar-btn {',
            '    position: relative !important; display: inline-flex !important; align-items: center !important; justify-content: center !important;',
            '    width: 32px !important; height: 32px !important; min-width: 32px !important; min-height: 32px !important; border-radius: 50% !important;',
            '    background: transparent !important; color: #d4d4d8 !important; margin: 0 !important; cursor: pointer !important;',
            '    border: none !important; outline: none !important; padding: 0 !important;',
            '    transition: transform 0.14s ease, background 0.14s ease, color 0.14s ease !important;',
            '}',
            '.pt-playbar-btn:hover {',
            '    transform: scale(1.08) !important; background: rgba(255, 255, 255, 0.1) !important; color: #ffffff !important;',
            '}',
            '.pt-playbar-btn.active {',
            '    background: rgba(0, 164, 220, 0.24) !important; color: #38bdf8 !important;',
            '}',
            '.pt-playbar-btn.danger.active {',
            '    background: rgba(239, 68, 68, 0.25) !important; color: #f87171 !important;',
            '}',
            '.pt-playbar-badge {',
            '    position: absolute !important; top: -2px !important; right: -2px !important; font-size: 9px !important; font-weight: 700 !important;',
            '    border-radius: 9px !important; padding: 1px 4px !important; min-width: 12px !important; text-align: center !important;',
            '    line-height: 1.2 !important; box-shadow: 0 1px 4px rgba(0, 0, 0, 0.6) !important; border: 1px solid rgba(255, 255, 255, 0.25) !important;',
            '    pointer-events: none !important;',
            '}',
            '/* Playlist Triage Header Shortcut Button & Icon */',
            '#pt-top-nav-link {',
            '    cursor: pointer !important; text-decoration: none !important;',
            '    transition: background-color 0.15s ease, color 0.15s ease !important;',
            '}',
            '#pt-top-nav-link:hover {',
            '    background-color: rgba(255, 255, 255, 0.08) !important;',
            '}',
            '.pt-top-header-btn {',
            '    cursor: pointer !important; opacity: 0.85 !important; transition: opacity 0.15s ease !important;',
            '}',
            '.pt-top-header-btn:hover {',
            '    opacity: 1 !important;',
            '}',
            '.pt-header-settings-btn {',
            '    display: inline-flex !important; align-items: center !important; justify-content: center !important;',
            '    cursor: pointer !important;',
            '}',
            '/* Swapped Controls: Volume on Left, Triage in Middle, Media Transport on Right */',
            'body.pt-controls-swapped .videoOsdBottom .buttons.focuscontainer-x {',
            '    display: flex !important; align-items: center !important; width: 100% !important; box-sizing: border-box !important;',
            '}',
            'body.pt-controls-swapped .videoOsdBottom .volumeButtons {',
            '    order: 1 !important; margin-right: 10px !important; margin-left: 0 !important; display: inline-flex !important; align-items: center !important;',
            '}',
            'body.pt-controls-swapped .videoOsdBottom #pt-playbar-capsule {',
            '    order: 2 !important; margin-right: 10px !important; margin-left: 0 !important; display: inline-flex !important;',
            '}',
            'body.pt-controls-swapped .videoOsdBottom .osdTimeText {',
            '    order: 3 !important; margin-right: auto !important;',
            '}',
            'body.pt-controls-swapped .videoOsdBottom .buttons.focuscontainer-x > div[dir="ltr"] {',
            '    order: 20 !important; margin-right: 8px !important; margin-left: auto !important;',
            '}',
            'body.pt-controls-swapped .videoOsdBottom .btnUserRating,',
            'body.pt-controls-swapped .videoOsdBottom .btnSubtitles,',
            'body.pt-controls-swapped .videoOsdBottom .btnAudio,',
            'body.pt-controls-swapped .videoOsdBottom .btnVideoOsdSettings,',
            'body.pt-controls-swapped .videoOsdBottom .btnAirPlay,',
            'body.pt-controls-swapped .videoOsdBottom .btnPip,',
            'body.pt-controls-swapped .videoOsdBottom .btnFullscreen {',
            '    order: 30 !important;',
            '}',
            '/* Default Controls Mode */',
            'body:not(.pt-controls-swapped) .videoOsdBottom .buttons.focuscontainer-x {',
            '    display: flex !important; align-items: center !important; width: 100% !important; box-sizing: border-box !important;',
            '}',
            'body:not(.pt-controls-swapped) .videoOsdBottom .buttons.focuscontainer-x > div[dir="ltr"] { order: 1 !important; }',
            'body:not(.pt-controls-swapped) .videoOsdBottom .osdTimeText { order: 2 !important; margin-right: auto !important; }',
            'body:not(.pt-controls-swapped) .videoOsdBottom #pt-playbar-capsule { order: 15 !important; margin: 0 5px !important; display: inline-flex !important; }',
            'body:not(.pt-controls-swapped) .videoOsdBottom .btnUserRating { order: 16 !important; }',
            'body:not(.pt-controls-swapped) .videoOsdBottom .btnSubtitles { order: 17 !important; }',
            'body:not(.pt-controls-swapped) .videoOsdBottom .btnAudio { order: 18 !important; }',
            'body:not(.pt-controls-swapped) .videoOsdBottom .volumeButtons { order: 19 !important; }',
            'body:not(.pt-controls-swapped) .videoOsdBottom .btnVideoOsdSettings { order: 20 !important; }',
            'body:not(.pt-controls-swapped) .videoOsdBottom .btnAirPlay,',
            'body:not(.pt-controls-swapped) .videoOsdBottom .btnPip,',
            'body:not(.pt-controls-swapped) .videoOsdBottom .btnFullscreen { order: 25 !important; }',
            '/* Clean Playbar options */',
            'body.pt-hide-prevnext .videoOsdBottom .btnPreviousTrack,',
            'body.pt-hide-prevnext .videoOsdBottom .btnPreviousChapter,',
            'body.pt-hide-prevnext .videoOsdBottom .btnNextChapter,',
            'body.pt-hide-prevnext .videoOsdBottom .btnNextTrack { display: none !important; }',
            'body.pt-hide-stop .videoOsdBottom .btnStop { display: none !important; }',
            '/* Library Filter Chips */',
            '.pt-filter-chip-bar {',
            '    display: flex; flex-wrap: wrap; align-items: center; gap: 8px; margin: 8px 0 16px 0; padding: 4px 0;',
            '}',
            '.pt-filter-chip {',
            '    display: inline-flex; align-items: center; gap: 6px; padding: 5px 12px; border-radius: 999px;',
            '    background: rgba(255, 255, 255, 0.06); border: 1px solid rgba(255, 255, 255, 0.12); color: #e4e4e7;',
            '    font-size: 12.5px; font-weight: 500; cursor: pointer; transition: all 0.16s ease;',
            '}',
            '.pt-filter-chip:hover {',
            '    background: rgba(255, 255, 255, 0.12); color: #fff; border-color: rgba(255, 255, 255, 0.25);',
            '}',
            '.pt-filter-chip.active {',
            '    background: #00a4dc; color: #ffffff; border-color: #00a4dc; font-weight: 600;',
            '}',
            '.pt-filter-chip.danger.active {',
            '    background: #dc2626; color: #ffffff; border-color: #dc2626;',
            '}'
        ].join('\n');
    }

    function loadServerConfig(cb) {
        ensureGlobalStyleTag();
        if (typeof ApiClient === 'undefined' || !ApiClient.ajax) {
            if (cb) cb(CONFIG);
            return;
        }

        ApiClient.ajax({
            type: 'GET',
            url: getApiUrl('Configuration?_t=' + Date.now()),
            dataType: 'json'
        }).then(function (cfg) {
            hasPluginBackend = true;
            if (cfg) {
                var defName = prop(cfg, 'defaultPlaylistName') || 'Triage Queue';
                if (defName.toLowerCase() === 'vidz') defName = 'Triage Queue';
                CONFIG.defaultPlaylistName = defName;
                CONFIG.configuredHotkey = prop(cfg, 'configuredHotkey') || 'Ctrl+Shift+B';
                CONFIG.swapOsdControls = !!prop(cfg, 'swapOsdControls');
                CONFIG.hideStopButton = !!prop(cfg, 'hideStopButton');
                CONFIG.hidePrevNextButtons = !!prop(cfg, 'hidePrevNextButtons');
                CONFIG.activeThemeCss = prop(cfg, 'activeThemeCss') || '';
                CONFIG.quickAccessPlaylists = prop(cfg, 'quickAccessPlaylists') || ['Triage Queue', 'Delete Queue'];
                var rawMappings = prop(cfg, 'playlistIconMappings') || [];
                CONFIG.playlistIconMappings = rawMappings.map(function (m) {
                    return {
                        playlistName: prop(m, 'playlistName') || '',
                        iconKey: prop(m, 'iconKey') || 'bookmark'
                    };
                });

                applyOsdSwapLayout(CONFIG.swapOsdControls);
                applyThemeCss(CONFIG.activeThemeCss);
                tryInjectPlaybarTriage();
            }
            if (cb) cb(CONFIG);
        }).catch(function () {
            hasPluginBackend = false;
            applyOsdSwapLayout(CONFIG.swapOsdControls);
            tryInjectPlaybarTriage();
            if (cb) cb(CONFIG);
        });
    }

    function showToast(message, badgeText, isError, isDanger, isRemoved) {
        var toastContainer = document.getElementById('pt-toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'pt-toast-container';
            toastContainer.style.cssText = 'position:fixed;top:20px;right:20px;z-index:1000005;display:flex;flex-direction:column;gap:8px;pointer-events:none;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;';
            document.body.appendChild(toastContainer);
        }

        var toast = document.createElement('div');
        var borderColor = isRemoved
            ? 'rgba(156, 163, 175, 0.4)'
            : (isDanger ? 'rgba(239, 68, 68, 0.4)' : (isError ? 'rgba(234, 179, 8, 0.4)' : 'rgba(0, 164, 220, 0.4)'));

        toast.style.cssText = 'background:#18181b;color:#f4f4f5;padding:8px 16px;border-radius:8px;border:1px solid ' + borderColor + ';box-shadow:0 8px 24px rgba(0,0,0,0.6);display:flex;align-items:center;gap:10px;font-size:13px;font-weight:500;opacity:0;transform:translateY(-8px);transition:opacity 0.18s ease, transform 0.18s ease;pointer-events:auto;';

        var iconHtml = isRemoved
            ? '<span style="color:#9ca3af;font-size:14px;">✕</span>'
            : (isDanger ? '<span style="color:#f87171;font-size:14px;">🗑</span>' : (isError ? '<span style="color:#facc15;font-size:14px;">⚠</span>' : '<span style="color:#00a4dc;font-size:14px;">✓</span>'));

        var badgeHtml = '';
        if (badgeText) {
            var bBg = isRemoved ? 'rgba(156,163,175,0.15)' : (isDanger ? 'rgba(239,68,68,0.15)' : 'rgba(0,164,220,0.15)');
            var bCol = isRemoved ? '#9ca3af' : (isDanger ? '#f87171' : '#38bdf8');
            badgeHtml = '<span style="background:' + bBg + ';color:' + bCol + ';border-radius:4px;padding:1px 6px;font-size:11px;font-weight:600;">' + badgeText + '</span>';
        }

        var msgSpan = document.createElement('span');
        msgSpan.textContent = message;

        toast.innerHTML = iconHtml;
        toast.appendChild(msgSpan);
        if (badgeHtml) {
            var temp = document.createElement('span');
            temp.innerHTML = badgeHtml;
            toast.appendChild(temp.firstChild);
        }

        toastContainer.appendChild(toast);

        requestAnimationFrame(function () {
            toast.style.opacity = '1';
            toast.style.transform = 'translateY(0)';
        });

        setTimeout(function () {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(-6px)';
            setTimeout(function () {
                if (toast.parentNode) toast.parentNode.removeChild(toast);
            }, 220);
        }, CONFIG.toastDurationMs);
    }

    function getCurrentItemInfo() {
        var item = null;
        try {
            // 1. Check window.playbackManager
            var pm = window.playbackManager;
            if (pm) {
                if (typeof pm.currentItem === 'function') {
                    item = pm.currentItem();
                } else if (pm.currentItem && typeof pm.currentItem === 'object') {
                    item = pm.currentItem;
                }
                if (!item && typeof pm.getCurrentItem === 'function') {
                    item = pm.getCurrentItem();
                }
                if (!item && pm.getCurrentPlayer && typeof pm.getCurrentPlayer === 'function') {
                    var player = pm.getCurrentPlayer();
                    if (player) {
                        if (typeof player.currentItem === 'function') item = player.currentItem();
                        else if (player.currentItem && typeof player.currentItem === 'object') item = player.currentItem;
                    }
                }
            }

            // 2. Check lastKnownPlayingItem from playback events
            if (!item && lastKnownPlayingItem) {
                item = lastKnownPlayingItem;
            }

            // 3. Firefox / Jellyfin 10.9 require module lookup
            if (!item && typeof window.require === 'function') {
                try {
                    var reqPm = window.require('playbackManager');
                    if (reqPm) {
                        if (typeof reqPm.currentItem === 'function') item = reqPm.currentItem();
                        else if (reqPm.currentItem && typeof reqPm.currentItem === 'object') item = reqPm.currentItem;
                    }
                } catch (e) {}
            }

            // 4. Jellyfin AppRouter currentRoute
            if (!item && window.appRouter && typeof window.appRouter.currentRouteInfo === 'function') {
                var routeInfo = window.appRouter.currentRouteInfo();
                if (routeInfo && routeInfo.item) {
                    item = routeInfo.item;
                }
            }
        } catch (e) {
            console.warn('[PlaylistTriage] playbackManager lookup error:', e);
        }

        var itemId = item ? (item.Id || item.id) : null;
        var itemName = item ? (item.Name || item.name) : null;

        // 5. Video/Audio media element inspection: dataset, src, currentSrc, source tags
        if (!itemId) {
            var videoEls = document.querySelectorAll('video, audio');
            for (var v = 0; v < videoEls.length; v++) {
                var vel = videoEls[v];
                if (vel.dataset && (vel.dataset.itemId || vel.dataset.itemid)) {
                    itemId = vel.dataset.itemId || vel.dataset.itemid;
                    break;
                }

                // Parse video src or currentSrc (e.g. /Videos/{GUID}/stream or /Audio/{GUID}/stream)
                var src = vel.currentSrc || vel.src || '';
                var m = src.match(/\/(?:Videos|Items|Audio)\/([a-f0-9]{32}|[a-f0-9-]{36})/i);
                if (m && m[1]) {
                    itemId = m[1].replace(/-/g, '');
                    break;
                }

                // Check child <source> elements
                var sources = vel.querySelectorAll('source');
                for (var s = 0; s < sources.length; s++) {
                    var sSrc = sources[s].getAttribute('src') || '';
                    var sm = sSrc.match(/\/(?:Videos|Items|Audio)\/([a-f0-9]{32}|[a-f0-9-]{36})/i);
                    if (sm && sm[1]) {
                        itemId = sm[1].replace(/-/g, '');
                        break;
                    }
                }
                if (itemId) break;

                // Check parent/closest item elements in DOM
                var itemParent = vel.closest('[data-id], [data-itemid]');
                if (itemParent) {
                    itemId = itemParent.getAttribute('data-id') || itemParent.getAttribute('data-itemid');
                    break;
                }
            }
        }

        // 6. Inspect nowPlaying DOM rating button and OSD elements (emby-ratingbutton always holds active item data-id)
        if (!itemId) {
            var ratingBtn = document.querySelector('.btnUserRating[data-id], [is="emby-ratingbutton"][data-id], .videoOsdPage [data-id], .videoOsdBottom [data-id]');
            if (ratingBtn) {
                var rId = ratingBtn.getAttribute('data-id');
                if (rId) itemId = rId;
            }
        }

        if (!itemId) {
            var osdLink = document.querySelector('.videoOsdPage a[data-id], .videoOsdPage a[href*="id="], .videoOsdBottom a[href*="id="]');
            if (osdLink) {
                var osdId = osdLink.getAttribute('data-id');
                if (!osdId) {
                    var href = osdLink.getAttribute('href') || '';
                    var lm = href.match(/[?&]id=([a-f0-9-]+)/i);
                    if (lm && lm[1]) osdId = lm[1];
                }
                if (osdId) itemId = osdId;
            }
        }

        // 7. Inspect router hash / URL parameters for itemId
        if (!itemId) {
            var hash = window.location.hash || '';
            var search = window.location.search || '';
            var match = hash.match(/[?&]id=([a-f0-9-]+)/i) ||
                        hash.match(/[?&]itemId=([a-f0-9-]+)/i) ||
                        search.match(/[?&]id=([a-f0-9-]+)/i) ||
                        search.match(/[?&]itemId=([a-f0-9-]+)/i);
            if (match && match[1]) {
                itemId = match[1];
            }
        }

        // 8. Resolve itemName from OSD title if missing
        if (!itemName) {
            var osdTitle = document.querySelector('.videoOsdBottom .videoOsdText, .videoOsdTitle, .videoOsdHeaderTitle, .videoOsdParentTitle, .osdTitle');
            if (osdTitle && osdTitle.textContent) {
                itemName = osdTitle.textContent.trim();
            }
        }

        return {
            item: item,
            itemId: itemId,
            itemName: itemName || (item ? (item.Name || item.name) : 'Active Media')
        };
    }

    function normalizeGuid(val) {
        if (!val) return '';
        return val.toString().replace(/[^a-f0-9]/gi, '').toLowerCase();
    }

    function isItemInPlaylist(playlistName, itemId) {
        if (!itemId) return false;
        var needle = normalizeGuid(itemId);
        if (!needle) return false;
        var lower = (playlistName || '').toLowerCase();
        var found = cachedPlaylists.find(function (p) {
            return (p.name || '').toLowerCase() === lower;
        });
        if (found && found.itemIds && Array.isArray(found.itemIds)) {
            return found.itemIds.some(function (id) {
                return normalizeGuid(id) === needle;
            });
        }
        return false;
    }

    /**
     * Toggles the currently playing media item in/out of the target queue
     */
    function toggleCurrentMedia(targetPlaylist) {
        if (typeof ApiClient === 'undefined') {
            showToast('Jellyfin ApiClient not ready', 'Error', true);
            return;
        }

        var playlistToUse = targetPlaylist || CONFIG.defaultPlaylistName || 'Triage Queue';
        var userId = getCurrentUserIdSafe();
        var deviceId = ApiClient.deviceId ? ApiClient.deviceId() : null;

        var info = getCurrentItemInfo();
        var itemId = info.itemId;
        var itemName = info.itemName;

        var lower = playlistToUse.toLowerCase();
        var isDeleteQueue = lower.indexOf('delete') !== -1 || lower.indexOf('trash') !== -1;

        if (hasPluginBackend === false) {
            fallbackNativeTogglePlaylist(playlistToUse, itemId, itemName, isDeleteQueue);
            return;
        }

        ApiClient.ajax({
            type: 'POST',
            url: getApiUrl('MarkCurrent'),
            data: JSON.stringify({
                PlaylistName: playlistToUse,
                UserId: userId || null,
                DeviceId: deviceId,
                ItemId: itemId
            }),
            contentType: 'application/json',
            dataType: 'json'
        }).then(function (res) {
            hasPluginBackend = true;
            var finalName = prop(res, 'itemName') || itemName || 'Media';
            var pName = prop(res, 'playlistName') || playlistToUse;
            var isRemoved = !!prop(res, 'removed') || prop(res, 'action') === 'removed';

            if (isRemoved) {
                showToast('Removed "' + finalName + '" from "' + pName + '"', 'Removed', false, isDeleteQueue, true);
            } else {
                var resLabel = prop(res, 'resolution');
                var sizeLabel = prop(res, 'formattedSize');
                var badge = [resLabel, sizeLabel].filter(Boolean).join(' • ');
                showToast('Added "' + finalName + '" to "' + pName + '"', badge || 'Queued', false, isDeleteQueue, false);
            }

            // Immediate optimistic client-side cache & button update
            var cached = cachedPlaylists.find(function (p) {
                return (p.name || '').toLowerCase() === pName.toLowerCase();
            });
            if (cached) {
                if (!cached.itemIds) cached.itemIds = [];
                var needle = normalizeGuid(itemId);
                if (isRemoved) {
                    cached.itemIds = cached.itemIds.filter(function (id) {
                        return normalizeGuid(id) !== needle;
                    });
                    cached.itemCount = Math.max(0, (cached.itemCount || 1) - 1);
                } else {
                    if (needle && !cached.itemIds.some(function (id) { return normalizeGuid(id) === needle; })) {
                        cached.itemIds.push(needle);
                    }
                    cached.itemCount = (cached.itemCount || 0) + 1;
                }
            }
            updatePlaybarButtons();

            // Background refresh to ensure perfect server synchronization
            refreshPlaylistCounts();
        }).catch(function (err) {
            console.warn('[PlaylistTriage] MarkCurrent returned error, executing native fallback:', err);
            if (itemId) {
                fallbackNativeTogglePlaylist(playlistToUse, itemId, itemName, isDeleteQueue);
            } else {
                var errMsg = (err && err.responseJSON && (err.responseJSON.message || err.responseJSON.Message)) || 'No media currently playing';
                showToast(errMsg, 'Playback Required', true, false);
            }
        });
    }

    /**
     * Fallback toggle for pure userscript mode without plugin backend
     */
    function fallbackNativeTogglePlaylist(playlistName, itemId, itemName, isDeleteQueue) {
        var userId = getCurrentUserIdSafe();
        if (!userId || !itemId) {
            showToast('No active playing media found', 'Playback Required', true);
            return;
        }

        ApiClient.getItems(userId, {
            includeItemTypes: 'Playlist',
            recursive: true,
            searchTerm: playlistName
        }).then(function (result) {
            var items = (result && result.Items) || [];
            var match = items.find(function (p) {
                return (p.Name || '').toLowerCase() === playlistName.toLowerCase();
            });

            if (!match) {
                // Playlist doesn't exist -> create and add
                ApiClient.ajax({
                    type: 'POST',
                    url: ApiClient.getUrl('Playlists', {
                        name: playlistName,
                        ids: itemId,
                        userId: userId
                    })
                }).then(function () {
                    showToast('Created "' + playlistName + '" & queued "' + itemName + '"', 'Queued', false, isDeleteQueue, false);
                    refreshPlaylistCounts();
                });
                return;
            }

            // Check if already in playlist
            ApiClient.getItems(userId, {
                parentId: match.Id
            }).then(function (subResult) {
                var subItems = (subResult && subResult.Items) || [];
                var existing = subItems.find(function (si) {
                    return (si.Id || '').toLowerCase() === itemId.toLowerCase();
                });

                if (existing) {
                    // Remove from playlist
                    var entryId = existing.PlaylistItemId || existing.Id;
                    ApiClient.ajax({
                        type: 'DELETE',
                        url: ApiClient.getUrl('Playlists/' + match.Id + '/Items', {
                            entryIds: entryId
                        })
                    }).then(function () {
                        showToast('Removed "' + itemName + '" from "' + playlistName + '"', 'Removed', false, isDeleteQueue, true);
                        var cached = cachedPlaylists.find(function (p) {
                            return (p.name || '').toLowerCase() === playlistName.toLowerCase();
                        });
                        if (cached) {
                            if (cached.itemIds) {
                                cached.itemIds = cached.itemIds.filter(function (id) {
                                    return (id || '').toString().toLowerCase() !== (itemId || '').toLowerCase();
                                });
                            }
                            cached.itemCount = Math.max(0, (cached.itemCount || 1) - 1);
                        }
                        updatePlaybarButtons();
                        refreshPlaylistCounts();
                    });
                } else {
                    // Add to playlist
                    ApiClient.ajax({
                        type: 'POST',
                        url: ApiClient.getUrl('Playlists/' + match.Id + '/Items', {
                            ids: itemId,
                            userId: userId
                        })
                    }).then(function () {
                        showToast('Added "' + itemName + '" to "' + playlistName + '"', 'Queued', false, isDeleteQueue, false);
                        var cached = cachedPlaylists.find(function (p) {
                            return (p.name || '').toLowerCase() === playlistName.toLowerCase();
                        });
                        if (cached) {
                            if (!cached.itemIds) cached.itemIds = [];
                            if (itemId && !cached.itemIds.some(function (id) { return (id || '').toString().toLowerCase() === itemId.toLowerCase(); })) {
                                cached.itemIds.push(itemId.toLowerCase());
                            }
                            cached.itemCount = (cached.itemCount || 0) + 1;
                        }
                        updatePlaybarButtons();
                        refreshPlaylistCounts();
                    });
                }
            });
        }).catch(function (err) {
            console.warn('[PlaylistTriage] Native fallback toggle error:', err);
            showToast('Could not query Jellyfin playlists', 'Error', true);
        });
    }

    function fetchPlaylists(callback) {
        var now = Date.now();
        if (cachedPlaylists.length > 0 && (now - lastPlaylistFetchTime < 4000)) {
            if (callback) callback(cachedPlaylists);
            return;
        }

        if (typeof ApiClient === 'undefined' || !ApiClient.ajax) {
            if (callback) callback([]);
            return;
        }

        if (hasPluginBackend === false) {
            fallbackNativeFetchPlaylists(callback);
            return;
        }

        ApiClient.ajax({
            type: 'GET',
            url: getApiUrl('Playlists?userId=' + getCurrentUserIdSafe() + '&_t=' + Date.now()),
            dataType: 'json'
        }).then(function (playlists) {
            hasPluginBackend = true;
            cachedPlaylists = (playlists || []).map(function (p) {
                var ids = prop(p, 'itemIds') || [];
                return {
                    id: prop(p, 'id'),
                    name: prop(p, 'name') || 'Playlist',
                    itemCount: prop(p, 'itemCount') || ids.length || 0,
                    itemIds: ids.map(function (x) { return (x || '').toString().toLowerCase(); }),
                    iconKey: prop(p, 'iconKey') || 'bookmark',
                    isDefault: !!prop(p, 'isDefault'),
                    isPinned: !!prop(p, 'isPinned')
                };
            });
            lastPlaylistFetchTime = now;
            if (callback) callback(cachedPlaylists);
        }).catch(function (err) {
            if (err && err.status === 404) {
                hasPluginBackend = false;
                fallbackNativeFetchPlaylists(callback);
            } else {
                if (callback) callback(cachedPlaylists);
            }
        });
    }

    function fallbackNativeFetchPlaylists(callback) {
        var userId = getCurrentUserIdSafe();
        if (!userId || typeof ApiClient === 'undefined' || !ApiClient.getItems) {
            if (callback) callback([]);
            return;
        }

        ApiClient.getItems(userId, {
            includeItemTypes: 'Playlist',
            recursive: true
        }).then(function (result) {
            var items = (result && result.Items) || [];
            cachedPlaylists = items.map(function (p) {
                return {
                    id: p.Id,
                    name: p.Name,
                    itemCount: p.ChildCount || 0,
                    itemIds: [],
                    iconKey: 'bookmark',
                    isDefault: (p.Name || '').toLowerCase() === (CONFIG.defaultPlaylistName || '').toLowerCase(),
                    isPinned: true
                };
            });
            lastPlaylistFetchTime = Date.now();
            if (callback) callback(cachedPlaylists);
        }).catch(function () {
            if (callback) callback(cachedPlaylists);
        });
    }

    function refreshPlaylistCounts() {
        lastPlaylistFetchTime = 0;
        fetchPlaylists(function () {
            updatePlaybarButtons();
            injectLibraryFilterChips();
        });
    }

    function getPlaylistIconSvg(playlistName, explicitIconKey) {
        if (explicitIconKey && ICONS[explicitIconKey]) {
            return ICONS[explicitIconKey];
        }
        var found = cachedPlaylists.find(function (p) {
            return (p.name || '').toLowerCase() === (playlistName || '').toLowerCase();
        });
        if (found && found.iconKey && ICONS[found.iconKey]) {
            return ICONS[found.iconKey];
        }
        var mapping = CONFIG.playlistIconMappings.find(function (m) {
            return (m.playlistName || '').toLowerCase() === (playlistName || '').toLowerCase();
        });
        var key = mapping ? mapping.iconKey : 'bookmark';
        return ICONS[key] || ICONS.bookmark;
    }

    function applyOsdSwapLayout(swap) {
        document.body.classList.toggle('pt-controls-swapped', !!swap);
        document.body.classList.toggle('pt-hide-stop', !!CONFIG.hideStopButton);
        document.body.classList.toggle('pt-hide-prevnext', !!CONFIG.hidePrevNextButtons);

        var swapBtn = document.getElementById('btnPtSwapControls');
        if (swapBtn) {
            swapBtn.style.color = swap ? '#38bdf8' : '#d4d4d8';
            swapBtn.title = swap ? 'Controls Swapped (Click to reset)' : 'Swap Controls (Play controls Right ↔ Volume Left)';
        }
    }

    function toggleOsdSwap() {
        CONFIG.swapOsdControls = !CONFIG.swapOsdControls;
        applyOsdSwapLayout(CONFIG.swapOsdControls);
        if (hasPluginBackend !== false && typeof ApiClient !== 'undefined' && ApiClient.ajax) {
            ApiClient.ajax({
                type: 'POST',
                url: getApiUrl('ToggleOsdSwap?swap=' + CONFIG.swapOsdControls)
            }).catch(function () {});
        }
        showToast(CONFIG.swapOsdControls ? 'Volume Left ↔ Controls Right' : 'Controls Reset to Default', 'OSD Layout');
    }

    function getBottomControlsContainer() {
        var osdBottom = document.querySelector('#videoOsdPage:not(.hide) .videoOsdBottom') ||
                        document.querySelector('.videoOsdBottom');
        if (!osdBottom) return null;

        return osdBottom.querySelector('.buttons.focuscontainer-x') ||
               osdBottom.querySelector('.videoOsdBottom-maincontrols') ||
               osdBottom.querySelector('.buttons') ||
               osdBottom;
    }

    function tryInjectPlaybarTriage() {
        var buttonsContainer = getBottomControlsContainer();
        if (!buttonsContainer) return;

        applyOsdSwapLayout(CONFIG.swapOsdControls);

        var existingCapsule = document.getElementById('pt-playbar-capsule');
        if (existingCapsule) {
            if (buttonsContainer.contains(existingCapsule)) {
                updatePlaybarButtons();
                return;
            }
            if (existingCapsule.parentNode) {
                existingCapsule.parentNode.removeChild(existingCapsule);
            }
        }

        var capsule = document.createElement('div');
        capsule.id = 'pt-playbar-capsule';
        capsule.className = 'pt-triage-capsule';

        capsule.innerHTML = [
            '<div id="pt-quick-buttons-wrapper" style="display:inline-flex;align-items:center;gap:3px;"></div>',
            '<button is="paper-icon-button-light" type="button" id="btnPtQuickMenu" class="paper-icon-button-light emby-button pt-playbar-btn" title="Open Cnidocil Queue Options">',
            ICONS.plus,
            '</button>',
            '<button is="paper-icon-button-light" type="button" id="btnPtSwapControls" class="paper-icon-button-light emby-button pt-playbar-btn" title="Swap Controls (Play controls Right ↔ Volume Left)">',
            ICONS.swap,
            '</button>',
            '<button is="paper-icon-button-light" type="button" id="btnPtOpenConfigModal" class="paper-icon-button-light emby-button pt-playbar-btn" title="Configure Overlay, Icons & Hotkey">',
            ICONS.settings,
            '</button>'
        ].join('');

        var anchor = buttonsContainer.querySelector('.btnUserRating') ||
                     buttonsContainer.querySelector('.btnAudio') ||
                     buttonsContainer.querySelector('.btnSubtitles') ||
                     buttonsContainer.querySelector('.btnSettings') ||
                     buttonsContainer.querySelector('.btnVideoOsdSettings') ||
                     buttonsContainer.querySelector('.btnFullscreen') ||
                     null;

        if (anchor && anchor.parentNode === buttonsContainer) {
            buttonsContainer.insertBefore(capsule, anchor);
        } else {
            buttonsContainer.appendChild(capsule);
        }

        document.getElementById('btnPtQuickMenu').onclick = function (e) {
            e.stopPropagation();
            openQuickReassignPopover(this);
        };
        document.getElementById('btnPtSwapControls').onclick = function (e) {
            e.stopPropagation();
            toggleOsdSwap();
        };
        document.getElementById('btnPtOpenConfigModal').onclick = function (e) {
            e.stopPropagation();
            openConfigModal();
        };

        fetchPlaylists(function () {
            updatePlaybarButtons();
        });
    }

    function updatePlaybarButtons() {
        var wrapper = document.getElementById('pt-quick-buttons-wrapper');
        if (!wrapper) return;

        wrapper.innerHTML = '';

        var currentInfo = getCurrentItemInfo();
        var currentItemId = currentInfo ? currentInfo.itemId : null;

        var pinned = cachedPlaylists.filter(function (p) { return p.isPinned; });
        var playlistsToShow = pinned.length > 0
            ? pinned.map(function (p) { return p.name; })
            : (CONFIG.quickAccessPlaylists && CONFIG.quickAccessPlaylists.length > 0 ? CONFIG.quickAccessPlaylists : [CONFIG.defaultPlaylistName]);

        playlistsToShow.forEach(function (playlistName) {
            var lower = playlistName.toLowerCase();
            var isDelete = lower.indexOf('delete') !== -1 || lower.indexOf('trash') !== -1;
            var found = cachedPlaylists.find(function (p) { return (p.name || '').toLowerCase() === lower; });
            var count = found ? found.itemCount : 0;
            var inQueue = isItemInPlaylist(playlistName, currentItemId);
            var iconSvg = getPlaylistIconSvg(playlistName, found ? found.iconKey : null);

            var btn = document.createElement('button');
            btn.setAttribute('is', 'paper-icon-button-light');
            btn.type = 'button';
            btn.className = 'paper-icon-button-light emby-button pt-playbar-btn' + (isDelete ? ' danger' : '') + (inQueue ? ' active' : '');
            btn.title = (inQueue ? '✓ Currently in "' + playlistName + '" (Click to Remove)' : 'Click to add to "' + playlistName + '"') + ' • ' + count + ' items';

            var badgeHtml = count > 0
                ? '<span class="pt-playbar-badge" style="background:' + (isDelete ? '#dc2626' : '#0284c7') + ';color:#fff;">' + count + '</span>'
                : '';

            btn.innerHTML = iconSvg + badgeHtml;

            btn.onclick = function (e) {
                e.stopPropagation();
                toggleCurrentMedia(playlistName);
            };

            btn.oncontextmenu = function (e) {
                e.preventDefault();
                e.stopPropagation();
                if (isDelete) {
                    openPurgeConfirmationModal(playlistName);
                } else {
                    openQuickReassignPopover(btn);
                }
            };

            wrapper.appendChild(btn);
        });
    }

    function closeModals() {
        var ids = ['pt-modal-purge-queue-backdrop', 'pt-config-modal-backdrop', 'pt-quick-reassign-popover'];
        ids.forEach(function (id) {
            var el = document.getElementById(id);
            if (el && el.parentNode) el.parentNode.removeChild(el);
        });
    }

    /**
     * Simplistic, Native-Themed Quick Triage Popover
     */
    function openQuickReassignPopover(anchorElement) {
        closeModals();

        var currentInfo = getCurrentItemInfo();
        var currentItemId = currentInfo ? currentInfo.itemId : null;

        var popover = document.createElement('div');
        popover.id = 'pt-quick-reassign-popover';
        popover.style.cssText = 'position:fixed;background:#18181b;border:1px solid rgba(255,255,255,0.12);border-radius:12px;padding:16px;color:#f4f4f5;box-shadow:0 16px 36px rgba(0,0,0,0.7);z-index:1000002;width:320px;max-width:92vw;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;font-size:13px;';

        if (anchorElement && anchorElement.getBoundingClientRect) {
            var rect = anchorElement.getBoundingClientRect();
            var topPos = rect.top - 340;
            if (topPos < 16) topPos = Math.min(window.innerHeight - 360, rect.bottom + 10);
            popover.style.top = Math.max(16, topPos) + 'px';
            popover.style.left = Math.min(window.innerWidth - 340, Math.max(16, rect.left - 100)) + 'px';
        } else {
            popover.style.top = '50%';
            popover.style.left = '50%';
            popover.style.transform = 'translate(-50%, -50%)';
        }

        popover.innerHTML = [
            '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;padding-bottom:8px;border-bottom:1px solid rgba(255,255,255,0.08);">',
            '  <div>',
            '    <div style="font-weight:600;font-size:14px;color:#fff;">Cnidocil Queue Manager</div>',
            '    <div style="font-size:11px;color:#a1a1aa;">Click any queue to toggle (add/remove)</div>',
            '  </div>',
            '  <button type="button" id="pt-btn-close-popover" style="background:transparent;border:none;color:#a1a1aa;width:24px;height:24px;border-radius:4px;cursor:pointer;font-size:14px;">✕</button>',
            '</div>',
            '<div id="pt-playlist-popover-list" style="max-height:180px;overflow-y:auto;display:flex;flex-direction:column;gap:5px;margin-bottom:12px;">',
            '  <div style="color:#71717a;text-align:center;padding:12px;">Loading queues...</div>',
            '</div>',
            '<div style="background:rgba(255,255,255,0.03);border:1px solid rgba(255,255,255,0.06);border-radius:8px;padding:8px 10px;margin-bottom:10px;">',
            '  <div style="font-size:11px;font-weight:600;color:#d4d4d8;margin-bottom:6px;">+ New Queue</div>',
            '  <div style="display:flex;gap:6px;margin-bottom:6px;">',
            '    <input id="pt-new-popover-playlist" type="text" placeholder="Queue name..." style="flex:1;background:#27272a;border:1px solid rgba(255,255,255,0.12);border-radius:4px;padding:5px 8px;color:#fff;font-size:12px;outline:none;" />',
            '    <select id="pt-new-popover-icon" style="background:#27272a;border:1px solid rgba(255,255,255,0.12);border-radius:4px;padding:4px;color:#fff;font-size:12px;outline:none;">',
            '      <option value="bookmark">🔖 Bookmark</option>',
            '      <option value="trash">🗑️ Trash</option>',
            '      <option value="star">⭐ Star</option>',
            '      <option value="clock">⏱️ Clock</option>',
            '      <option value="film">🎬 Film</option>',
            '      <option value="fire">🔥 Fire</option>',
            '    </select>',
            '  </div>',
            '  <div style="display:flex;justify-content:flex-end;">',
            '    <button type="button" id="pt-btn-add-popover-playlist" style="background:#00a4dc;color:#fff;border:none;border-radius:4px;padding:4px 10px;font-size:11.5px;font-weight:600;cursor:pointer;">+ Add Queue</button>',
            '  </div>',
            '</div>',
            '<div style="display:flex;justify-content:space-between;gap:6px;">',
            '  <button type="button" id="pt-btn-popover-swap" style="flex:1;background:rgba(255,255,255,0.06);border:1px solid rgba(255,255,255,0.1);color:#d4d4d8;border-radius:6px;padding:6px;font-size:11px;font-weight:500;cursor:pointer;">⇄ Swap Controls</button>',
            '  <button type="button" id="pt-btn-popover-dashboard" style="flex:1;background:rgba(0,164,220,0.15);border:1px solid rgba(0,164,220,0.3);color:#38bdf8;border-radius:6px;padding:6px;font-size:11px;font-weight:600;cursor:pointer;">Open Settings →</button>',
            '</div>'
        ].join('');

        document.body.appendChild(popover);

        document.getElementById('pt-btn-close-popover').onclick = closeModals;
        document.getElementById('pt-btn-popover-swap').onclick = function () {
            toggleOsdSwap();
        };
        document.getElementById('pt-btn-popover-dashboard').onclick = function () {
            closeModals();
            window.location.hash = '#/configurationpage?name=PlaylistTriage';
        };

        function renderPopoverPlaylists(playlists) {
            var container = document.getElementById('pt-playlist-popover-list');
            if (!container) return;
            container.innerHTML = '';

            playlists.forEach(function (p) {
                var lower = (p.name || '').toLowerCase();
                var isDelete = lower.indexOf('delete') !== -1 || lower.indexOf('trash') !== -1;
                var inQueue = isItemInPlaylist(p.name, currentItemId);

                var row = document.createElement('div');
                row.style.cssText = 'padding:6px 8px;border-radius:6px;cursor:pointer;display:flex;justify-content:space-between;align-items:center;background:' + (inQueue ? 'rgba(0,164,220,0.18)' : 'rgba(255,255,255,0.04)') + ';border:1px solid ' + (inQueue ? 'rgba(0,164,220,0.4)' : 'transparent') + ';transition:all 0.14s ease;';

                var left = document.createElement('div');
                left.style.cssText = 'display:flex;align-items:center;gap:6px;';
                var iconSpan = document.createElement('span');
                iconSpan.style.cssText = 'color:' + (isDelete ? '#f87171' : '#38bdf8') + ';display:inline-flex;';
                iconSpan.innerHTML = getPlaylistIconSvg(p.name, p.iconKey);
                var nameSpan = document.createElement('span');
                nameSpan.style.cssText = 'font-weight:500;color:#fff;font-size:12.5px;';
                nameSpan.textContent = p.name;
                left.appendChild(iconSpan);
                left.appendChild(nameSpan);

                var right = document.createElement('div');
                right.style.cssText = 'display:flex;align-items:center;gap:6px;';

                if (inQueue) {
                    var inBadge = document.createElement('span');
                    inBadge.style.cssText = 'background:rgba(0,164,220,0.25);color:#38bdf8;font-size:10px;padding:1px 5px;border-radius:4px;font-weight:600;';
                    inBadge.textContent = 'In Queue';
                    right.appendChild(inBadge);
                }

                var countSpan = document.createElement('span');
                countSpan.style.cssText = 'color:#a1a1aa;font-size:11px;';
                countSpan.textContent = p.itemCount + '';
                right.appendChild(countSpan);

                if (p.itemCount > 0) {
                    var purgeBtn = document.createElement('button');
                    purgeBtn.type = 'button';
                    purgeBtn.title = 'Permanently purge all ' + p.itemCount + ' items in "' + p.name + '" from disk storage';
                    purgeBtn.style.cssText = 'background:rgba(239,68,68,0.18);border:1px solid rgba(239,68,68,0.35);color:#f87171;border-radius:4px;padding:2px 6px;font-size:10.5px;font-weight:600;cursor:pointer;display:inline-flex;align-items:center;gap:3px;margin-left:4px;line-height:1.2;';
                    purgeBtn.innerHTML = '🗑 Purge';
                    purgeBtn.onmouseover = function () { this.style.background = 'rgba(239,68,68,0.35)'; };
                    purgeBtn.onmouseout = function () { this.style.background = 'rgba(239,68,68,0.18)'; };
                    purgeBtn.onclick = function (e) {
                        e.stopPropagation();
                        openPurgeConfirmationModal(p.name);
                    };
                    right.appendChild(purgeBtn);
                }

                row.appendChild(left);
                row.appendChild(right);

                row.onclick = function () {
                    toggleCurrentMedia(p.name);
                    setTimeout(function () {
                        fetchPlaylists(renderPopoverPlaylists);
                    }, 300);
                };
                container.appendChild(row);
            });
        }

        fetchPlaylists(renderPopoverPlaylists);

        document.getElementById('pt-btn-add-popover-playlist').onclick = function () {
            var nameInput = document.getElementById('pt-new-popover-playlist');
            var iconSelect = document.getElementById('pt-new-popover-icon');
            var newName = (nameInput.value || '').trim();
            if (!newName) return;

            ApiClient.ajax({
                type: 'POST',
                url: getApiUrl('CreatePlaylist'),
                data: JSON.stringify({
                    Name: newName,
                    IconKey: iconSelect.value || 'bookmark',
                    PinToOsd: true,
                    SetAsDefault: false,
                    UserId: getCurrentUserIdSafe() || null
                }),
                contentType: 'application/json',
                dataType: 'json'
            }).then(function () {
                nameInput.value = '';
                showToast('Created "' + newName + '"', 'Created');
                refreshPlaylistCounts();
                setTimeout(function () {
                    fetchPlaylists(renderPopoverPlaylists);
                }, 300);
            });
        };
    }

    function openPurgeConfirmationModal(playlistName) {
        closeModals();

        var backdrop = document.createElement('div');
        backdrop.id = 'pt-modal-purge-queue-backdrop';
        backdrop.style.cssText = 'position:fixed;top:0;left:0;width:100vw;height:100vh;background:rgba(0,0,0,0.8);backdrop-filter:blur(6px);z-index:1000004;display:flex;align-items:center;justify-content:center;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;';

        backdrop.innerHTML = [
            '<div style="background:#18181b;border:1px solid rgba(239,68,68,0.35);border-radius:12px;max-width:420px;width:90%;padding:20px;box-shadow:0 20px 50px rgba(0,0,0,0.8);color:#fff;">',
            '  <div style="display:flex;align-items:center;gap:10px;margin-bottom:12px;">',
            '    <span style="color:#f87171;font-size:20px;">🗑</span>',
            '    <div>',
            '      <h3 style="margin:0;color:#f87171;font-size:16px;font-weight:600;">Purge Queue: <span id="pt-purge-modal-title-name"></span></h3>',
            '      <div style="font-size:11.5px;color:#a1a1aa;">Permanent Media Deletion</div>',
            '    </div>',
            '  </div>',
            '  <p style="font-size:13px;line-height:1.5;color:#d4d4d8;margin-bottom:16px;">',
            '    Are you sure you want to permanently delete all items in queue <strong id="pt-purge-modal-body-name" style="color:#fff;background:rgba(239,68,68,0.22);padding:2px 7px;border-radius:4px;border:1px solid rgba(239,68,68,0.35);"></strong> from physical disk storage? This cannot be undone.',
            '  </p>',
            '  <div style="display:flex;justify-content:flex-end;gap:8px;">',
            '    <button type="button" id="pt-btn-cancel-purge" style="background:rgba(255,255,255,0.06);border:1px solid rgba(255,255,255,0.12);color:#e4e4e7;padding:6px 14px;border-radius:6px;cursor:pointer;font-size:12.5px;">Cancel</button>',
            '    <button type="button" id="pt-btn-confirm-purge" style="background:#dc2626;border:none;color:#fff;padding:6px 16px;border-radius:6px;font-weight:600;cursor:pointer;font-size:12.5px;">Permanently Delete</button>',
            '  </div>',
            '</div>'
        ].join('');

        document.body.appendChild(backdrop);

        var titleNameEl = document.getElementById('pt-purge-modal-title-name');
        if (titleNameEl) titleNameEl.textContent = playlistName;
        var bodyNameEl = document.getElementById('pt-purge-modal-body-name');
        if (bodyNameEl) bodyNameEl.textContent = '"' + playlistName + '"';

        document.getElementById('pt-btn-cancel-purge').onclick = closeModals;
        document.getElementById('pt-btn-confirm-purge').onclick = function () {
            if (typeof ApiClient === 'undefined') return;

            ApiClient.ajax({
                type: 'POST',
                url: getApiUrl('PurgeEntireQueue'),
                data: JSON.stringify({
                    PlaylistName: playlistName,
                    ConfirmPurge: true,
                    UserId: getCurrentUserIdSafe() || null
                }),
                contentType: 'application/json',
                dataType: 'json'
            }).then(function (res) {
                closeModals();
                var count = prop(res, 'resolvedCount') || 0;
                var freed = prop(res, 'formattedBytesFreed') || '';
                var freedMsg = freed ? ' (' + freed + ' freed)' : '';
                showToast('Purged ' + count + ' items from storage' + freedMsg, 'Purged', false, true);
                refreshPlaylistCounts();
            }).catch(function (err) {
                closeModals();
                var msg = (err && err.responseJSON && err.responseJSON.message) || 'Purge requires Admin privileges';
                showToast(msg, 'Error', true, true);
            });
        };
    }

    function openConfigModal() {
        closeModals();

        var backdrop = document.createElement('div');
        backdrop.id = 'pt-config-modal-backdrop';
        backdrop.style.cssText = 'position:fixed;top:0;left:0;width:100vw;height:100vh;background:rgba(0,0,0,0.8);backdrop-filter:blur(6px);z-index:1000003;display:flex;align-items:center;justify-content:center;font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif;';

        backdrop.innerHTML = [
            '<div style="background:#18181b;border:1px solid rgba(255,255,255,0.12);border-radius:12px;max-width:480px;width:90%;padding:20px;box-shadow:0 20px 60px rgba(0,0,0,0.8);color:#fff;">',
            '  <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:14px;">',
            '    <h3 style="margin:0;color:#00a4dc;font-size:16px;font-weight:600;">Cnidocil Settings</h3>',
            '    <button type="button" id="pt-btn-close-config" style="background:transparent;border:none;cursor:pointer;color:#a1a1aa;font-size:16px;">✕</button>',
            '  </div>',
            '  <div style="margin-bottom:14px;padding-bottom:12px;border-bottom:1px solid rgba(255,255,255,0.08);">',
            '    <label style="display:flex;align-items:center;gap:8px;cursor:pointer;margin-bottom:8px;">',
            '      <input type="checkbox" id="pt-cb-swap-controls" ' + (CONFIG.swapOsdControls ? 'checked' : '') + ' />',
            '      <span style="font-size:13px;font-weight:500;">Swap Player Controls (Volume Left ↔ Play Right)</span>',
            '    </label>',
            '    <label style="display:flex;align-items:center;gap:8px;cursor:pointer;margin-bottom:8px;">',
            '      <input type="checkbox" id="pt-cb-hide-stop" ' + (CONFIG.hideStopButton ? 'checked' : '') + ' />',
            '      <span style="font-size:12.5px;color:#a1a1aa;">Clean Playbar: Hide Stop Button</span>',
            '    </label>',
            '    <label style="display:flex;align-items:center;gap:8px;cursor:pointer;">',
            '      <input type="checkbox" id="pt-cb-hide-prevnext" ' + (CONFIG.hidePrevNextButtons ? 'checked' : '') + ' />',
            '      <span style="font-size:12.5px;color:#a1a1aa;">Clean Playbar: Hide Previous / Next Buttons</span>',
            '    </label>',
            '  </div>',
            '  <div style="margin-bottom:16px;">',
            '    <label style="display:block;font-size:12px;font-weight:500;color:#a1a1aa;margin-bottom:6px;">Keyboard Shortcut:</label>',
            '    <div style="display:flex;gap:6px;">',
            '      <input id="pt-input-hotkey" type="text" value="' + escapeHtml(CONFIG.configuredHotkey) + '" style="flex:1;background:#27272a;border:1px solid rgba(255,255,255,0.12);border-radius:6px;padding:6px 10px;color:#fff;font-family:monospace;font-size:12px;" />',
            '      <button type="button" id="pt-btn-save-config-modal" style="background:#00a4dc;border:none;color:#fff;padding:6px 14px;border-radius:6px;font-weight:600;font-size:12px;cursor:pointer;">Save</button>',
            '    </div>',
            '  </div>',
            '  <div style="padding-top:12px;border-top:1px solid rgba(255,255,255,0.08);display:flex;justify-content:space-between;align-items:center;">',
            '    <button type="button" id="pt-btn-modal-open-dash" style="background:rgba(0,164,220,0.15);border:1px solid rgba(0,164,220,0.35);color:#38bdf8;padding:6px 12px;border-radius:6px;font-size:12px;font-weight:600;cursor:pointer;">Open Full Cnidocil Dashboard →</button>',
            '    <button type="button" id="pt-btn-modal-close" style="background:transparent;border:1px solid rgba(255,255,255,0.12);color:#a1a1aa;padding:6px 12px;border-radius:6px;font-size:12px;cursor:pointer;">Close</button>',
            '  </div>',
            '</div>'
        ].join('');

        document.body.appendChild(backdrop);

        document.getElementById('pt-btn-close-config').onclick = closeModals;
        var btnModalClose = document.getElementById('pt-btn-modal-close');
        if (btnModalClose) btnModalClose.onclick = closeModals;
        var btnModalDash = document.getElementById('pt-btn-modal-open-dash');
        if (btnModalDash) {
            btnModalDash.onclick = function () {
                closeModals();
                window.location.hash = '#/configurationpage?name=PlaylistTriage';
            };
        }

        var hotkeyInput = document.getElementById('pt-input-hotkey');
        hotkeyInput.onkeydown = function (e) {
            e.preventDefault();
            var keys = [];
            if (e.ctrlKey) keys.push('Ctrl');
            if (e.metaKey) keys.push('Cmd');
            if (e.altKey) keys.push('Alt');
            if (e.shiftKey) keys.push('Shift');
            var k = (e.key || '').toUpperCase();
            if (k !== 'CONTROL' && k !== 'ALT' && k !== 'SHIFT' && k !== 'META') keys.push(k);
            if (keys.length > 0) hotkeyInput.value = keys.join('+');
        };

        document.getElementById('pt-btn-save-config-modal').onclick = function () {
            CONFIG.swapOsdControls = document.getElementById('pt-cb-swap-controls').checked;
            CONFIG.hideStopButton = document.getElementById('pt-cb-hide-stop').checked;
            CONFIG.hidePrevNextButtons = document.getElementById('pt-cb-hide-prevnext').checked;
            CONFIG.configuredHotkey = (hotkeyInput.value || '').trim() || 'Ctrl+Shift+B';

            applyOsdSwapLayout(CONFIG.swapOsdControls);

            if (hasPluginBackend !== false && typeof ApiClient !== 'undefined' && ApiClient.ajax) {
                ApiClient.ajax({
                    type: 'POST',
                    url: getApiUrl('SaveOverlayPreferences'),
                    data: JSON.stringify({
                        SwapOsdControls: CONFIG.swapOsdControls,
                        HideStopButton: CONFIG.hideStopButton,
                        HidePrevNextButtons: CONFIG.hidePrevNextButtons,
                        ShowFloatingQuickOverlay: false,
                        ConfiguredHotkey: CONFIG.configuredHotkey
                    }),
                    contentType: 'application/json'
                }).then(function () {
                    closeModals();
                    showToast('Settings saved', 'Saved');
                });
            } else {
                closeModals();
                showToast('Settings saved locally', 'Saved');
            }
        };
    }

    /**
     * Injects the Triage shortcut icon & link into the top navigation header bar.
     * Supports both modern Jellyfin Web (MUI AppBar & Toolbar) and classic Jellyfin Web (skinHeader).
     */
    function tryInjectHeaderTriageShortcut() {
        // 1. Modern Jellyfin Web (MUI Navigation Bar)
        var stack = document.querySelector('.MuiToolbar-root .MuiStack-root');
        if (stack && !document.getElementById('pt-top-nav-link')) {
            var a = document.createElement('a');
            a.id = 'pt-top-nav-link';
            a.className = 'MuiButtonBase-root MuiButton-root MuiButton-text MuiButton-textInherit MuiButton-sizeMedium MuiButton-textSizeMedium MuiButton-colorInherit css-1f20jcn pt-header-triage-link';
            a.tabIndex = 0;
            a.href = '#/configurationpage?name=PlaylistTriage';
            a.title = 'Cnidocil Settings & Dashboard';
            a.style.cursor = 'pointer';
            a.innerHTML = [
                '<span class="MuiButton-icon MuiButton-startIcon MuiButton-iconSizeMedium css-1ygddt1">',
                '  <svg class="MuiSvgIcon-root MuiSvgIcon-fontSizeMedium css-iguwhy" focusable="false" aria-hidden="true" viewBox="0 0 24 24" style="fill:currentColor;width:20px;height:20px;">',
                '    <path d="M17 3H7c-1.1 0-2 .9-2 2v16l7-3 7 3V5c0-1.1-.9-2-2-2zm0 15-5-2.18L7 18V5h10v13z"/>',
                '  </svg>',
                '</span>',
                'Cnidocil'
            ].join('');
            a.onclick = function (e) {
                window.location.hash = '#/configurationpage?name=PlaylistTriage';
            };
            stack.appendChild(a);
        }

        // 2. Modern Jellyfin Web (MUI Right Icon Box - Quick Settings Modal)
        var iconBox = document.querySelector('.MuiToolbar-root .MuiBox-root');
        if (iconBox && !document.getElementById('pt-top-icon-btn')) {
            var btn = document.createElement('button');
            btn.id = 'pt-top-icon-btn';
            btn.type = 'button';
            btn.className = 'MuiButtonBase-root MuiIconButton-root MuiIconButton-colorInherit MuiIconButton-sizeLarge css-i2hxb6 pt-top-header-btn';
            btn.title = 'Cnidocil Settings';
            btn.tabIndex = 0;
            btn.style.cursor = 'pointer';
            btn.innerHTML = [
                '<svg class="MuiSvgIcon-root MuiSvgIcon-fontSizeMedium css-iguwhy" focusable="false" aria-hidden="true" viewBox="0 0 24 24" style="fill:currentColor;width:20px;height:20px;">',
                '  <path d="M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"/>',
                '</svg>'
            ].join('');
            btn.onclick = function (e) {
                e.stopPropagation();
                openConfigModal();
            };
            iconBox.insertBefore(btn, iconBox.firstChild);
        }

        // 3. Classic Jellyfin Web (skinHeader)
        var skinHeader = document.querySelector('.skinHeader');
        if (skinHeader) {
            var headerRight = skinHeader.querySelector('.headerRight');
            if (headerRight && !document.getElementById('pt-classic-header-btn')) {
                var classicBtn = document.createElement('button');
                classicBtn.id = 'pt-classic-header-btn';
                classicBtn.type = 'button';
                classicBtn.setAttribute('is', 'paper-icon-button-light');
                classicBtn.className = 'headerButton headerButtonRight paper-icon-button-light emby-button pt-header-settings-btn';
                classicBtn.title = 'Cnidocil Settings';
                classicBtn.innerHTML = '<span class="material-icons" style="font-size:20px;">tune</span>';
                classicBtn.onclick = function (e) {
                    e.stopPropagation();
                    openConfigModal();
                };
                headerRight.insertBefore(classicBtn, headerRight.firstChild);
            }

            var tabsSlider = skinHeader.querySelector('.emby-tabs-slider');
            if (tabsSlider && !document.getElementById('pt-classic-tabs-btn')) {
                var tabBtn = document.createElement('button');
                tabBtn.id = 'pt-classic-tabs-btn';
                tabBtn.type = 'button';
                tabBtn.setAttribute('is', 'emby-button');
                tabBtn.className = 'emby-tab-button emby-button pt-header-tab';
                tabBtn.title = 'Cnidocil Settings & Dashboard';
                tabBtn.innerHTML = '<div class="emby-button-foreground">Cnidocil</div>';
                tabBtn.onclick = function () {
                    window.location.hash = '#/configurationpage?name=PlaylistTriage';
                };
                tabsSlider.appendChild(tabBtn);
            }
        }
    }

    /**
     * Library Queue Filtering ("My Media" Integration)
     * Injects Triage Queues into the native filter slideout / dialog, and adds quick filter chips.
     */
    function tryInjectQueueFiltersIntoNativeDialog() {
        var dialog = document.querySelector('.filterDialog') ||
                     document.querySelector('.filterDialogContent') ||
                     document.querySelector('.dialogContent .filterDialog') ||
                     document.querySelector('[data-role="dialog"]');

        if (!dialog) return;

        var content = dialog.querySelector('.filterDialogContent') || dialog;
        if (content.querySelector('.pt-queue-filter-section')) {
            return;
        }

        var section = document.createElement('div');
        section.className = 'filterSection pt-queue-filter-section';
        section.innerHTML = [
            '<h2 class="sectionTitle filterSectionTitle">Triage Queues</h2>',
            '<div class="filterOptions pt-queue-filter-options"></div>'
        ].join('');

        var optionsContainer = section.querySelector('.pt-queue-filter-options');

        cachedPlaylists.forEach(function (p) {
            var lower = (p.name || '').toLowerCase();
            var isDelete = lower.indexOf('delete') !== -1 || lower.indexOf('trash') !== -1;
            var count = p.itemCount || (p.itemIds ? p.itemIds.length : 0);

            var label = document.createElement('label');
            label.className = 'emby-checkbox-label';

            var cb = document.createElement('input');
            cb.type = 'checkbox';
            cb.setAttribute('is', 'emby-checkbox');
            cb.className = 'emby-checkbox pt-queue-filter-cb';
            cb.dataset.queueName = p.name;
            cb.checked = activeQueueFilters.indexOf(p.name) !== -1;

            cb.onchange = function () {
                if (cb.checked) {
                    if (activeQueueFilters.indexOf(p.name) === -1) activeQueueFilters.push(p.name);
                } else {
                    activeQueueFilters = activeQueueFilters.filter(function (n) { return n !== p.name; });
                }
                applyQueueFilterToLibraryCards();
                updateChipBarActiveStates();
            };

            var span = document.createElement('span');
            span.className = 'checkboxLabel';
            span.textContent = p.name + ' (' + count + ')';
            if (isDelete) span.style.color = '#f87171';

            var outline = document.createElement('span');
            outline.className = 'checkboxOutline';
            outline.innerHTML = '<span class="material-icons checkboxIcon checkboxIcon-checked" aria-hidden="true">check</span><span class="material-icons checkboxIcon checkboxIcon-unchecked" aria-hidden="true"></span>';

            label.appendChild(cb);
            label.appendChild(span);
            label.appendChild(outline);
            optionsContainer.appendChild(label);
        });

        var firstSection = content.querySelector('.filterSection') || content.firstElementChild;
        if (firstSection && firstSection.parentNode === content) {
            content.insertBefore(section, firstSection.nextSibling);
        } else {
            content.appendChild(section);
        }
    }

    function injectLibraryFilterChips() {
        var page = document.querySelector('.page:not(.hide)');
        if (!page) return;

        // Only show filter chips on library/items pages (Movies, Series, Collections)
        var isLibraryPage = page.classList.contains('itemsPage') ||
                            document.querySelector('.itemsContainer') ||
                            (window.location.hash && (window.location.hash.indexOf('items') !== -1 || window.location.hash.indexOf('movies') !== -1 || window.location.hash.indexOf('tv') !== -1));

        if (!isLibraryPage) {
            var existingChipBar = document.getElementById('pt-library-filter-chips');
            if (existingChipBar && existingChipBar.parentNode) existingChipBar.parentNode.removeChild(existingChipBar);
            return;
        }

        var targetContainer = page.querySelector('.viewSettings') ||
                              page.querySelector('.alphaPicker') ||
                              page.querySelector('.itemsContainer')?.parentNode ||
                              page;

        if (!targetContainer) return;

        var chipBar = document.getElementById('pt-library-filter-chips');
        if (!chipBar) {
            chipBar = document.createElement('div');
            chipBar.id = 'pt-library-filter-chips';
            chipBar.className = 'pt-filter-chip-bar';

            var anchor = targetContainer.querySelector('.itemsContainer') || targetContainer.firstChild;
            targetContainer.insertBefore(chipBar, anchor);
        }

        chipBar.innerHTML = '';

        // "All" chip
        var allChip = document.createElement('button');
        allChip.type = 'button';
        allChip.className = 'pt-filter-chip' + (activeQueueFilters.length === 0 ? ' active' : '');
        allChip.textContent = 'All Media';
        allChip.onclick = function () {
            activeQueueFilters = [];
            applyQueueFilterToLibraryCards();
            updateChipBarActiveStates();
        };
        chipBar.appendChild(allChip);

        // Queue chips
        cachedPlaylists.forEach(function (p) {
            var count = p.itemCount || (p.itemIds ? p.itemIds.length : 0);
            if (count === 0 && activeQueueFilters.indexOf(p.name) === -1) return;

            var lower = p.name.toLowerCase();
            var isDelete = lower.indexOf('delete') !== -1 || lower.indexOf('trash') !== -1;
            var isAct = activeQueueFilters.indexOf(p.name) !== -1;

            var chip = document.createElement('button');
            chip.type = 'button';
            chip.className = 'pt-filter-chip' + (isDelete ? ' danger' : '') + (isAct ? ' active' : '');
            chip.dataset.queueName = p.name;

            var iconHtml = isDelete ? '🗑 ' : '🔖 ';
            chip.textContent = iconHtml + p.name + ' (' + count + ')';

            chip.onclick = function () {
                if (isAct) {
                    activeQueueFilters = activeQueueFilters.filter(function (n) { return n !== p.name; });
                } else {
                    activeQueueFilters = [p.name]; // single click filter
                }
                applyQueueFilterToLibraryCards();
                updateChipBarActiveStates();
            };

            chipBar.appendChild(chip);
        });
    }

    function updateChipBarActiveStates() {
        var chipBar = document.getElementById('pt-library-filter-chips');
        if (!chipBar) return;

        var chips = chipBar.querySelectorAll('.pt-filter-chip');
        chips.forEach(function (chip) {
            var qName = chip.dataset.queueName;
            if (!qName) {
                chip.classList.toggle('active', activeQueueFilters.length === 0);
            } else {
                chip.classList.toggle('active', activeQueueFilters.indexOf(qName) !== -1);
            }
        });
    }

    function applyQueueFilterToLibraryCards() {
        var cards = document.querySelectorAll('.card');
        if (!cards || cards.length === 0) return;

        if (!activeQueueFilters || activeQueueFilters.length === 0) {
            cards.forEach(function (c) {
                c.style.display = '';
            });
            return;
        }

        var allowedIds = new Set();
        activeQueueFilters.forEach(function (qName) {
            var found = cachedPlaylists.find(function (p) {
                return (p.name || '').toLowerCase() === qName.toLowerCase();
            });
            if (found && found.itemIds && Array.isArray(found.itemIds)) {
                found.itemIds.forEach(function (id) {
                    allowedIds.add((id || '').toLowerCase());
                });
            }
        });

        cards.forEach(function (c) {
            var cardId = c.getAttribute('data-id') || '';
            if (!cardId) {
                var link = c.querySelector('a[href*="id="]');
                if (link) {
                    var m = link.href.match(/id=([a-f0-9-]+)/i);
                    if (m) cardId = m[1];
                }
            }

            if (cardId && allowedIds.has(cardId.toLowerCase())) {
                c.style.display = '';
            } else {
                c.style.display = 'none';
            }
        });
    }

    // Hotkey listener
    window.addEventListener('keydown', function (e) {
        var activeTag = document.activeElement && document.activeElement.tagName ? document.activeElement.tagName.toLowerCase() : '';
        if (activeTag === 'input' || activeTag === 'textarea' || activeTag === 'select' || (document.activeElement && document.activeElement.isContentEditable)) {
            return;
        }

        if (isConfiguredHotkeyPressed(e, CONFIG.configuredHotkey)) {
            e.preventDefault();
            e.stopPropagation();
            toggleCurrentMedia(CONFIG.defaultPlaylistName);
        }
    }, true);

    function isConfiguredHotkeyPressed(e, hotkeyStr) {
        if (!hotkeyStr) return false;
        var tokens = hotkeyStr.toLowerCase().split('+').map(function (t) { return t.trim(); });
        var wantsCtrl = tokens.indexOf('ctrl') !== -1 || tokens.indexOf('control') !== -1;
        var wantsCmd = tokens.indexOf('cmd') !== -1 || tokens.indexOf('meta') !== -1;
        var wantsAlt = tokens.indexOf('alt') !== -1;
        var wantsShift = tokens.indexOf('shift') !== -1;

        var keyToken = tokens.find(function (t) {
            return t !== 'ctrl' && t !== 'control' && t !== 'cmd' && t !== 'meta' && t !== 'alt' && t !== 'shift';
        });
        if (!keyToken) return false;

        if (wantsCtrl !== (e.ctrlKey || (wantsCmd && e.metaKey))) return false;
        if (wantsAlt !== e.altKey) return false;
        if (wantsShift !== e.shiftKey) return false;

        var pressedKey = (e.key || '').toLowerCase();
        var pressedCode = (e.code || '').toLowerCase();
        return pressedKey === keyToken || pressedCode === ('key' + keyToken);
    }

    var isInjecting = false;
    function safeInjectPlaybar() {
        if (isInjecting) return;
        var activeContainer = getBottomControlsContainer();
        if (!activeContainer) return;
        if (activeContainer.querySelector('#pt-playbar-capsule')) return;
        isInjecting = true;
        try {
            tryInjectPlaybarTriage();
        } finally {
            isInjecting = false;
        }
    }

    var playbarTimer = null;
    function schedulePlaybarCheck(delay) {
        if (playbarTimer) clearTimeout(playbarTimer);
        playbarTimer = setTimeout(function () {
            safeInjectPlaybar();
        }, delay || 200);
    }

    // MutationObserver: detects OSD mount, filter dialogs, top nav, and library page navigations
    var domObserver = new MutationObserver(function () {
        var activeContainer = getBottomControlsContainer();
        if (activeContainer && !activeContainer.querySelector('#pt-playbar-capsule')) {
            schedulePlaybarCheck(50);
        }
        if (document.querySelector('.filterDialog, .filterDialogContent')) {
            tryInjectQueueFiltersIntoNativeDialog();
        }
        tryInjectHeaderTriageShortcut();
    });

    function startObservers() {
        if (document.body) {
            domObserver.observe(document.body, { childList: true, subtree: true });
        } else {
            setTimeout(startObservers, 200);
        }
    }

    // Lifecycle events
    window.addEventListener('playbackstart', function () {
        schedulePlaybarCheck(200);
        schedulePlaybarCheck(600);
        refreshPlaylistCounts();
    });

    window.addEventListener('play', function () {
        schedulePlaybarCheck(150);
    });

    window.addEventListener('hashchange', function () {
        activeQueueFilters = [];
        tryInjectHeaderTriageShortcut();
        if (window.location.hash && window.location.hash.indexOf('video') !== -1) {
            schedulePlaybarCheck(250);
        } else {
            setTimeout(injectLibraryFilterChips, 300);
        }
    });

    document.addEventListener('viewshow', function () {
        tryInjectHeaderTriageShortcut();
        if (window.location.hash && window.location.hash.indexOf('video') !== -1) {
            schedulePlaybarCheck(250);
        } else {
            setTimeout(injectLibraryFilterChips, 300);
        }
    });

    // Public extension helpers
    window.PlaylistTriageOpenOverlay = function (anchor) {
        openQuickReassignPopover(anchor || null);
    };
    window.PlaylistTriageOpenSettings = function () {
        openConfigModal();
    };
    window.PlaylistTriageRefresh = function () {
        loadServerConfig(function () {
            refreshPlaylistCounts();
        });
    };
    window.PlaylistTriageApplyTheme = function (css) {
        CONFIG.activeThemeCss = css || '';
        applyThemeCss(CONFIG.activeThemeCss);
    };

    var apiWaitAttempts = 0;
    function waitForApiClientAndLoad() {
        if (typeof ApiClient !== 'undefined' && ApiClient.ajax) {
            loadServerConfig(function () {
                refreshPlaylistCounts();
                schedulePlaybarCheck(300);
            });
        } else if (apiWaitAttempts < 50) {
            apiWaitAttempts++;
            setTimeout(waitForApiClientAndLoad, 300);
        }
    }

    var bootAttempts = 0;
    function bootstrap() {
        if (!isJellyfinEnvironment()) {
            if (bootAttempts < 30) {
                bootAttempts++;
                setTimeout(bootstrap, 300);
            }
            return;
        }
        console.log('[Cnidocil] Initialized (Queue Toggle & Media Filter Enabled).');
        startObservers();
        ensureGlobalStyleTag();
        tryInjectHeaderTriageShortcut();
        setInterval(tryInjectHeaderTriageShortcut, 2000);
        waitForApiClientAndLoad();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();
