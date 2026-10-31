#!/usr/bin/env bash
# ==============================================================================
# Jellyfin Playlist Triage & Pruner - Mac Quick Installer
# Double-click this script in macOS Finder to install.
# ==============================================================================

cd "$(dirname "$0")"

echo "================================================================="
echo "   Jellyfin Playlist Triage & Pruner - Mac Quick Installer       "
echo "================================================================="
echo ""

# 1. Locate ALL Jellyfin Data/Plugin Directories on Mac (.NET 8 uses ~/.local/share/jellyfin, macOS app uses ~/Library/Application Support/jellyfin)
BASE_JELLYFIN_DIRS=(
    "$HOME/.local/share/jellyfin"
    "$HOME/Library/Application Support/jellyfin"
    "/Library/Application Support/jellyfin"
)

INSTALLED_COUNT=0

for base in "${BASE_JELLYFIN_DIRS[@]}"; do
    if [ -d "$base" ]; then
        PLUGIN_DIR="$base/plugins"
        CONFIG_DIR="$PLUGIN_DIR/configurations"
        mkdir -p "$PLUGIN_DIR"
        mkdir -p "$CONFIG_DIR"

        # Remove any old or versioned folders (e.g. PlaylistTriage_1.2.0.0 or "Playlist Triage & Media Pruner_1.2.0.0") so Jellyfin never loads a stale DLL
        rm -rf "$PLUGIN_DIR"/PlaylistTriage* "$PLUGIN_DIR"/"Playlist Triage"* 2>/dev/null || true

        DEST="$PLUGIN_DIR/PlaylistTriage"
        mkdir -p "$DEST"

        cp -v PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.dll "$DEST/"
        if [ -f "PlaylistTriage/meta.json" ]; then
            cp -v PlaylistTriage/meta.json "$DEST/"
        fi
        if [ -f "PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.pdb" ]; then
            cp -v PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.pdb "$DEST/"
        fi

        # Ensure full read/write permissions on both plugin and configurations folders and remove macOS quarantine flags
        chmod -R 755 "$DEST" "$CONFIG_DIR" 2>/dev/null || true
        chown -R "$(id -un):$(id -gn)" "$DEST" "$CONFIG_DIR" 2>/dev/null || true
        xattr -cr "$DEST" 2>/dev/null || true

        echo " -> Installed cleanly to: $DEST"
        INSTALLED_COUNT=$((INSTALLED_COUNT + 1))
    fi
done

if [ $INSTALLED_COUNT -eq 0 ]; then
    # Default fallback if neither base dir existed yet
    for base in "$HOME/.local/share/jellyfin" "$HOME/Library/Application Support/jellyfin"; do
        DEST="$base/plugins/PlaylistTriage"
        CONFIG_DIR="$base/plugins/configurations"
        mkdir -p "$DEST" "$CONFIG_DIR"
        cp -v PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.dll "$DEST/"
        [ -f "PlaylistTriage/meta.json" ] && cp -v PlaylistTriage/meta.json "$DEST/"
        [ -f "PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.pdb" ] && cp -v PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.pdb "$DEST/"
        chmod -R 755 "$DEST" "$CONFIG_DIR" 2>/dev/null || true
        echo " -> Installed to: $DEST"
    done
fi

echo ""

# 2. Check for Jellyfin Web index.html on Mac
echo "[2/2] Checking Jellyfin Web Client for automatic UI activation..."
WEB_PATHS=(
    "/Applications/Jellyfin.app/Contents/Resources/jellyfin-web/index.html"
    "/opt/homebrew/share/jellyfin-web/index.html"
    "/usr/local/share/jellyfin-web/index.html"
    "$HOME/.local/share/jellyfin/web/index.html"
    "$HOME/Library/Application Support/jellyfin/web/index.html"
)

FOUND_WEB=""
for w in "${WEB_PATHS[@]}"; do
    if [ -f "$w" ]; then
        FOUND_WEB="$w"
        break
    fi
done

INJECT_TAG='<script src="/Plugins/PlaylistTriage/ClientScript" defer></script>'

if [ -n "$FOUND_WEB" ]; then
    echo "Found Jellyfin Web entry file: $FOUND_WEB"
    if grep -q "PlaylistTriage" "$FOUND_WEB"; then
        echo " -> Client script is already active in index.html!"
    else
        echo " -> Automatically injecting client script tag into index.html..."
        cp "$FOUND_WEB" "$FOUND_WEB.bak" 2>/dev/null || true
        sed -i '' "s|</body>|$INJECT_TAG</body>|g" "$FOUND_WEB" 2>/dev/null || true
        echo " -> [SUCCESS] Injected!"
    fi
else
    echo " -> Note: Standalone index.html not found at standard Mac app paths."
    echo "    (If you use JS Injector or Userscript, see MAC_INSTRUCTIONS.txt)."
fi

echo ""
echo "================================================================="
echo "                  INSTALLATION COMPLETED                         "
echo "================================================================="
echo ""
if pgrep -i jellyfin >/dev/null 2>&1; then
    echo "⚠️  CRITICAL: Jellyfin is CURRENTLY RUNNING on your Mac!"
    echo "    macOS keeps the old plugin DLL loaded in memory until the"
    echo "    server process is quit."
    echo ""
    echo "    YOU MUST COMPLETELY QUIT AND RESTART JELLYFIN NOW"
    echo "    for the updated plugin and playlist fixes to take effect!"
else
    echo "IMPORTANT NEXT STEPS:"
    echo "1. Start your Jellyfin Server application on macOS."
fi
echo "2. In your Mac browser, press Cmd + Shift + R to hard-refresh."
echo ""
read -p "Press Return to close this window..."
