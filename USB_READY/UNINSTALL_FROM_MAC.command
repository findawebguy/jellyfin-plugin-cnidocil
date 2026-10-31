#!/usr/bin/env bash
# ==============================================================================
# Jellyfin Playlist Triage & Pruner - Mac Uninstaller
# Double-click this script in macOS Finder to completely remove the plugin.
# ==============================================================================

cd "$(dirname "$0")"

echo "================================================================="
echo "   Jellyfin Playlist Triage & Pruner - Mac Uninstaller           "
echo "================================================================="
echo ""

BASE_DIRS=(
    "$HOME/.local/share/jellyfin/plugins"
    "$HOME/Library/Application Support/jellyfin/plugins"
    "/Library/Application Support/jellyfin/plugins"
    "/Applications/Jellyfin.app/Contents/Resources/plugins"
)

for d in "${BASE_DIRS[@]}"; do
    if [ -d "$d" ]; then
        echo "Cleaning plugin folders in: $d"
        rm -rf "$d"/PlaylistTriage* "$d"/"Playlist Triage"* 2>/dev/null || true
        rm -f "$d/configurations/Jellyfin.Plugin.PlaylistTriage.xml" 2>/dev/null || true
    fi
done

# Check index.html to clean up injection tag if present
WEB_PATHS=(
    "/Applications/Jellyfin.app/Contents/Resources/jellyfin-web/index.html"
    "/opt/homebrew/share/jellyfin-web/index.html"
    "/usr/local/share/jellyfin-web/index.html"
    "$HOME/.local/share/jellyfin/web/index.html"
    "$HOME/Library/Application Support/jellyfin/web/index.html"
)

for w in "${WEB_PATHS[@]}"; do
    if [ -f "$w" ] && grep -q "PlaylistTriage" "$w"; then
        echo "Cleaning injected tag from: $w"
        sed -i '' 's|<script src="/Plugins/PlaylistTriage/ClientScript"></script>||g' "$w" 2>/dev/null || true
        sed -i '' 's|<script src="/Plugins/PlaylistTriage/ClientScript" defer></script>||g' "$w" 2>/dev/null || true
    fi
done

echo ""
echo "================================================================="
echo "  [SUCCESS] Playlist Triage was completely uninstalled!"
echo "  Please restart your Jellyfin Server to complete uninstallation."
echo "================================================================="
echo ""
read -p "Press Return to close this window..."
