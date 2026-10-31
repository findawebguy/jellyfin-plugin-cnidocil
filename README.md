# Cnidocil (jellyfin-plugin-cnidocil)

Production-ready Jellyfin Server Plugin (.NET 8) and Web Client Extension for high-speed media triage, playlist auto-routing, and safe disk-level library pruning.

* **GitHub Repository**: [https://github.com/findawebguy/jellyfin-plugin-cnidocil](https://github.com/findawebguy/jellyfin-plugin-cnidocil)
* **Official Repository**: [jellyfin-plugin-cnidocil](https://github.com/findawebguy/jellyfin-plugin-cnidocil)
* **Plugin GUID**: `b14f8263-5e92-4f6b-871d-5a9e7019c4b2`
* **Gitea Repository**: [http://192.168.86.32:3000/findawebguy/jellyfin-plugin-cnidocil.git](http://192.168.86.32:3000/findawebguy/jellyfin-plugin-cnidocil.git)

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
└── USB_READY/                                 # Ready-to-copy offline USB installer package (Windows & Mac)
    ├── INSTALL_ON_WINDOWS.bat                 # 1-Click Windows PC auto-installer
    ├── UNINSTALL_ON_WINDOWS.bat               # 1-Click Windows PC auto-uninstaller
    ├── INSTALL_ON_MAC.command                 # 1-Click macOS Finder auto-installer
    ├── UNINSTALL_FROM_MAC.command             # 1-Click macOS Finder auto-uninstaller
    ├── PlaylistTriage/                        # Compiled .NET 8 DLL & meta.json
    ├── Jellyfin.Plugin.PlaylistTriage.zip     # Compressed plugin bundle
    ├── triageClient.js                        # Client injection script
    └── Jellyfin-PlaylistTriage.user.js        # Browser userscript
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

### 🖥️ Windows PC Installation

#### Method A: 1-Click USB / Batch Installer (Recommended)
From the [`USB_READY`](./USB_READY) directory:
1. Double-click [`INSTALL_ON_WINDOWS.bat`](./USB_READY/INSTALL_ON_WINDOWS.bat).
2. The installer will automatically:
   - Search standard Windows Jellyfin server paths:
     - `%ProgramData%\Jellyfin\Server` (Default Windows Service / Installer)
     - `%LocalAppData%\Jellyfin` (User-level / Portable)
     - `%ProgramFiles%\Jellyfin\Server`
   - Clean any stale or duplicate plugin builds (`PlaylistTriage*`).
   - Create the target `plugins\PlaylistTriage` folder and deploy `Jellyfin.Plugin.PlaylistTriage.dll` and `meta.json`.
   - Automatically check for local `jellyfin-web\index.html` and append the client script tag if present.
3. Restart your Jellyfin Server (right-click the Jellyfin tray icon and select **Exit & Restart**, or restart the service via `services.msc`).

#### Method B: Manual Windows PC Installation
1. Stop your Jellyfin Server.
2. Open Windows File Explorer (or press `Win + R`) and go to:
   ```cmd
   %ProgramData%\Jellyfin\Server\plugins
   ```
   *(If running portable, use `%LocalAppData%\Jellyfin\plugins`)*
3. Create a new folder named `PlaylistTriage`.
4. Copy the following files from `dist/PlaylistTriage/` (or `USB_READY/PlaylistTriage/`):
   - `Jellyfin.Plugin.PlaylistTriage.dll`
   - `meta.json`
5. Start Jellyfin.
6. Open your browser and navigate to your Jellyfin Web interface:
   - **Cnidocil** will appear under **Dashboard -> Plugins**.
   - The top navigation bar will display the **Cnidocil** tab and the playbar capsule will be active during video playback.

---

### 🍎 macOS Installation (1-Click USB Installer)
From the [`USB_READY`](./USB_READY) folder:
1. Double-click [`INSTALL_ON_MAC.command`](./USB_READY/INSTALL_ON_MAC.command).
2. It automatically scans `~/.local/share/jellyfin`, `~/Library/Application Support/jellyfin`, and `/Library/Application Support/jellyfin`, deploys the `.dll`, strips quarantine attributes (`xattr -cr`), and injects the client script into your Jellyfin Web distribution.
3. Restart your Jellyfin server.

---

### 🐧 Linux & Docker Installation (Server-Side)
1. Stop your Jellyfin container or service:
   ```bash
   sudo systemctl stop jellyfin
   # Or for docker:
   docker compose stop jellyfin
   ```
2. Locate your Jellyfin plugins directory:
   - **Linux System:** `/var/lib/jellyfin/plugins`
   - **Docker Host Volume:** Typically `./config/plugins` or `/mnt/user/appdata/jellyfin/plugins`
3. Create the plugin directory and copy the release files:
   ```bash
   mkdir -p /var/lib/jellyfin/plugins/PlaylistTriage
   cp dist/PlaylistTriage/Jellyfin.Plugin.PlaylistTriage.dll /var/lib/jellyfin/plugins/PlaylistTriage/
   cp dist/PlaylistTriage/meta.json /var/lib/jellyfin/plugins/PlaylistTriage/
   sudo chown -R jellyfin:jellyfin /var/lib/jellyfin/plugins/PlaylistTriage
   ```
4. Start Jellyfin.

---

### 🌐 Client-Only / Browser Extensions (For Viewers & Remote Users)

#### Option 1: Browser Userscript (Tampermonkey / Violentmonkey)
Use this if you are a user watching media on a Windows, Mac, or Linux browser and do not have administrative access to install server plugins:
1. Install [Tampermonkey](https://www.tampermonkey.net/) or Violentmonkey in Chrome, Edge, Firefox, or Brave.
2. Create a new script, paste the contents of [`Jellyfin-PlaylistTriage.user.js`](./Jellyfin-PlaylistTriage.user.js), and save (`Ctrl + S`).
3. Open your Jellyfin Web client—the playbar capsule, queue manager popover, and top navigation shortcut will activate automatically!

#### Option 2: Jellyfin JavaScript Injector Plugin
If your server already has the community **JavaScript Injector** plugin:
1. In Jellyfin Admin, go to **Dashboard -> Plugins -> JavaScript Injector**.
2. Click **Add Script**, paste the content of [`triageClient.js`](./triageClient.js), toggle it **Active**, and click **Save**.

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
