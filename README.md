# Cnidocil (jellyfin-plugin-cnidocil)

Production-ready Jellyfin Server Plugin (.NET 8) and Web Client Extension for high-speed media triage, playlist auto-routing, and safe disk-level library pruning.

* **GitHub Repository**: [https://github.com/findawebguy/jellyfin-plugin-cnidocil](https://github.com/findawebguy/jellyfin-plugin-cnidocil)
* **Subfolder of**: [Jellyfin Plugins Catalog](../README.md)
* **Plugin GUID**: `b14f8263-5e92-4f6b-871d-5a9e7019c4b2`
* **Gitea Repository**: [http://192.168.86.32:3000/findawebguy/jellyfin-plugins.git](http://192.168.86.32:3000/findawebguy/jellyfin-plugins.git)

---

## 🌟 Features

- **Queue Toggling (Add / Remove):**
  - Instant toggle action: clicking a playlist queue button or pressing the hotkey adds the active video if not in the queue, or removes it if already present.
  - Active queues display dynamic `✓ In Queue` status badges in the Quick-Triage popover.
- **Library Filter Integration ("My Media"):**
  - Injects a native collapsible `Triage Queues` section into Jellyfin's slideout filter menu (`.filterDialogContent`).
  - Real-time zero-latency card filtering by queue membership.
  - Includes quick filter pill chips (`.pt-filter-chip-bar`) pinned at the top of Movies/Series library pages for 1-click filtering.
- **Simplistic, Premium Dark Theme:**
  - Styled with Jellyfin's native charcoal palette (`#18181b`, `#202024`, `#00a4dc` accent) and clean borders.
  - Removed persistent floating dock from browsing windows; streamlined triage access to the in-player OSD and Settings navigation drawer.
- **Continuous Playback (Zero Glitch):**
  - Instant optimistic feedback toast without interrupting the video pipeline, audio decoder, or playback state.
  - Glassmorphic floating toast displays enriched media resolution (e.g. `1080p`, `4K UHD`) and file size.
- **Native Video Player Playbar Capsule (`#pt-playbar-capsule`):**
  - Styled capsule container (`display: inline-flex; align-items: center; gap: 4px;`) injected directly into `.videoOsdBottom .buttons.focuscontainer-x`.
  - Uses native `.paper-icon-button-light.emby-button` styles matching Jellyfin's exact look and feel.
  - **Automatic Idle Visibility Transitions**: Because the capsule is injected inside `.videoOsdBottom`, it inherits all native fade-in and fade-out idle opacity transitions automatically.
- **Live Queue Item Badges & Custom Icons:**
  - Shows real-time badge counters indicating how many items are in each playlist queue.
  - Custom SVG icons (🗑️ `trash`, 🔖 `bookmark`, ⭐ `star`, ✂️ `scissors`, 🔥 `fire`, ❤️ `heart`, 📦 `archive`, ⏱️ `clock`).
- **OSD Controls Swapping (Left ↔ Right):**
  - Instant 1-click toggle to swap Play/Media controls to the Right and Volume Slider to the Left for ergonomic mouse reach.
  - Clean playbar options to hide unused Stop or Previous/Next buttons.
- **Direct `window.playbackManager` & `window.ApiClient` Integration:**
  - Real-time resolution of current playing item ID, item name, and file stream.
  - **Dual API Engine**: Calls high-speed server plugin endpoints (`/Plugins/PlaylistTriage/MarkCurrent`), with graceful fallback to native Jellyfin playlist APIs (`ApiClient.createPlaylist` / `ApiClient.addToPlaylist`) when running in pure userscript mode.
- **Delete Entire Queue & Storage Purging:**
  - Purge all queued items in a playlist/queue from physical disk storage with 1 click directly from the player UI (right-click on delete queue icon) or management dashboard.
  - Enforces two-step confirmation and rigorous admin / deletion permission verification.
- **Configurable Keyboard Shortcut:**
  - Fully customizable hotkey (default: `Ctrl + Shift + B` / `Cmd + Shift + B`), configurable in real-time through the UI or plugin settings modal.
- **Dedicated Triage Management Dashboard:**
  - Located under Jellyfin Library / Plugin Pages (`#!/configurationpage?name=PlaylistTriage`).
  - Tabular view with multi-select checkboxes, posters, stream codecs, bitrates, file sizes, durations, and physical disk paths (with 1-click copy).

---

## 📁 Directory Structure

```
PlaylistTriage/
├── Jellyfin.Plugin.PlaylistTriage/
│   ├── Jellyfin.Plugin.PlaylistTriage.csproj  # .NET 8 Class Library targeting Jellyfin 10.9+
│   ├── Plugin.cs                              # Main plugin lifecycle & IHasWebPages
│   ├── Configuration/
│   │   └── PluginConfiguration.cs             # Config schema (default playlist, hotkey, toast ms)
│   ├── Api/
│   │   ├── TriageController.cs                # REST API: MarkCurrent, TriageList, ResolveBatch, PurgeEntireQueue
│   │   └── Models/
│   │       ├── MarkCurrentRequest.cs
│   │       ├── MarkCurrentResponse.cs
│   │       ├── TriageItemDto.cs
│   │       ├── TriageListResponse.cs
│   │       ├── ResolveBatchRequest.cs
│   │       ├── ResolveBatchResponse.cs
│   │       ├── QueueManagementModels.cs
│   │       └── PlaylistSummaryDto.cs
│   └── Web/
│       ├── triage.html                        # Embedded Management Dashboard UI
│       └── triageClient.js                    # Injected client hotkey & OSD extension
├── Jellyfin-PlaylistTriage.user.js            # Standalone Tampermonkey / Violentmonkey Userscript
├── triageClient.js                            # Standalone client script for custom JavaScript injection
├── manifest.json                              # Jellyfin plugin manifest
├── build.sh                                   # Build & package script for macOS / Linux
├── build.ps1                                  # Build & package script for Windows
├── docker-compose.yml                         # Containerized testing environment
├── run-docker-test.bat                        # 1-click test container startup script
├── recreate-docker-test.bat                   # 1-click container recreation & password reset script
├── dist/                                      # Compiled release artifacts (.zip, .dll, scripts)
└── USB_READY/                                 # Ready-to-copy offline USB installer package for Mac
```

---

## 🛠️ Build & Packaging

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Automated Build Commands

**On macOS / Linux:**
```bash
chmod +x build.sh
./build.sh
```

**On Windows (PowerShell):**
```powershell
.\build.ps1
```

The build output will generate:
- `dist/PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.dll`
- `dist/Jellyfin.Plugin.PlaylistTriage.zip`
- `dist/triageClient.js`
- `dist/Jellyfin-PlaylistTriage.user.js`

---

## 🚀 Installation Options

### Option 1: Native Plugin Installation (Server-Side)
1. Stop your Jellyfin server.
2. Locate your Jellyfin plugins directory:
   - **macOS:** `~/.local/share/jellyfin/plugins` or `/Library/Application Support/jellyfin/plugins`
   - **Docker / Container:** `/config/plugins`
   - **Linux:** `/var/lib/jellyfin/plugins`
   - **Windows:** `%ProgramData%\Jellyfin\Server\plugins`
3. Create a folder named `PlaylistTriage` inside the `plugins` directory.
4. Copy `dist/PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.dll` into the `PlaylistTriage` folder.
5. Start Jellyfin. The plugin will appear in **Dashboard -> Plugins**.

### Option 2: Browser Userscript (Tampermonkey / Violentmonkey)
1. Install [Tampermonkey](https://www.tampermonkey.net/) in your web browser.
2. Create a new userscript and paste the contents of [`Jellyfin-PlaylistTriage.user.js`](./Jellyfin-PlaylistTriage.user.js).
3. The script is configured with `// ==UserScript==` targeting all pages (`*://*/*`) and automatically verifies Jellyfin's presence before running.
4. Open Jellyfin Web. You will immediately see the playbar capsule and quick-triage dock during video playback!

### Option 3: Jellyfin JavaScript Injector Plugin
1. Install the **JavaScript Injector** plugin from the Jellyfin Catalog.
2. Go to **Dashboard -> Plugins -> JavaScript Injector**.
3. Click **Add Script**, paste the content of [`triageClient.js`](./triageClient.js), toggle it **Active**, and click **Save**.

### Option 4: macOS 1-Click USB Installer
From the [`USB_READY`](./USB_READY) folder:
* Plug your USB into the Mac and double-click [`INSTALL_ON_MAC.command`](./USB_READY/INSTALL_ON_MAC.command).
* It automatically deploys the `.dll` and injects the client script into your Jellyfin Web distribution.

---

## 🐳 Docker Test Environment & Password Reset

### Running the Test Container
* **Windows**: Double-click [`run-docker-test.bat`](./run-docker-test.bat) or run `docker compose up -d`.
* **URL**: [http://localhost:8096](http://localhost:8096)

### How to Reset the Root / Admin Password
> [!NOTE]
> Jellyfin does **not** support environment variables like `JELLYFIN_PASSWORD` in `docker-compose.yml`. User accounts are stored in an encrypted SQLite database.

To reset the admin password or recreate an admin without deleting media:
1. Double-click [`recreate-docker-test.bat`](./recreate-docker-test.bat) and choose option **`[2] Reset Admin Password / Rerun Setup Wizard`**.
2. Or perform manually:
   - Run `docker compose stop jellyfin`.
   - Open `test_env/config/config/system.xml`.
   - Change `<IsStartupWizardCompleted>true</IsStartupWizardCompleted>` to `<IsStartupWizardCompleted>false</IsStartupWizardCompleted>`.
   - Run `docker compose start jellyfin`.
   - Navigate to [http://localhost:8096](http://localhost:8096) to complete the setup wizard and set a new password.

### Chromium 129+ Browser Freeze Prevention
Both `triageClient.js` and `Jellyfin-PlaylistTriage.user.js` include a runtime patch for Chromium 129+ strict `ScrollOptions.behavior` checking, preventing browser freezes when navigating Jellyfin Web.
