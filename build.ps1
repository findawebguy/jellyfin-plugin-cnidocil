# ==============================================================================
# Jellyfin Playlist Triage & Pruner - Windows PowerShell Packaging Script
# ==============================================================================

$ProjectDir = "Jellyfin.Plugin.PlaylistTriage"
$OutputDir = "dist"
$PluginDirName = "PlaylistTriage"
$DllName = "Jellyfin.Plugin.PlaylistTriage.dll"

Write-Host "=== Building Jellyfin.Plugin.PlaylistTriage (.NET 8 Release) ===" -ForegroundColor Cyan
dotnet publish "$ProjectDir/$ProjectDir.csproj" -c Release -o "$ProjectDir/bin/Release/net8.0/publish"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host "=== Preparing Output Directory ===" -ForegroundColor Cyan
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Path "$OutputDir/$PluginDirName" -Force | Out-Null

Copy-Item "$ProjectDir/bin/Release/net8.0/publish/$DllName" "$OutputDir/$PluginDirName/"
if (Test-Path "$ProjectDir/bin/Release/net8.0/publish/*.pdb") {
    Copy-Item "$ProjectDir/bin/Release/net8.0/publish/*.pdb" "$OutputDir/$PluginDirName/"
}
if (Test-Path "$ProjectDir/meta.json") {
    Copy-Item "$ProjectDir/meta.json" "$OutputDir/$PluginDirName/"
}
Copy-Item "$ProjectDir/Web/triageClient.js" "$OutputDir/triageClient.js"
Copy-Item "$ProjectDir/Web/triageClient.js" "$OutputDir/Jellyfin-PlaylistTriage.user.js"
Copy-Item "$ProjectDir/Web/triageClient.js" "triageClient.js"
Copy-Item "$ProjectDir/Web/triageClient.js" "Jellyfin-PlaylistTriage.user.js"

Write-Host "=== Creating ZIP Archive ===" -ForegroundColor Cyan
Compress-Archive -Path "$OutputDir/$PluginDirName/*" -DestinationPath "$OutputDir/$ProjectDir.zip" -Force

$md5 = (Get-FileHash -Algorithm MD5 "$OutputDir/$ProjectDir.zip").Hash.ToLowerInvariant()
$sha256 = (Get-FileHash -Algorithm SHA256 "$OutputDir/$ProjectDir.zip").Hash.ToLowerInvariant()

Write-Host "==============================================================================" -ForegroundColor Green
Write-Host "Build & Packaging Successful!" -ForegroundColor Green
Write-Host "Output Zip:       $OutputDir/$ProjectDir.zip"
Write-Host "Plugin Directory: $OutputDir/$PluginDirName/"
Write-Host "Client Script:    $OutputDir/triageClient.js"
Write-Host "MD5 Checksum:     $md5"
Write-Host "SHA256 Checksum:  $sha256"
Write-Host "==============================================================================" -ForegroundColor Green
