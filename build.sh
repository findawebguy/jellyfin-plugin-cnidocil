#!/usr/bin/env bash
set -e

# ==============================================================================
# Jellyfin Playlist Triage & Pruner - Build & Package Script
# ==============================================================================

PROJECT_DIR="Jellyfin.Plugin.PlaylistTriage"
OUTPUT_DIR="dist"
PLUGIN_DIR_NAME="PlaylistTriage"
DLL_NAME="Jellyfin.Plugin.PlaylistTriage.dll"

echo "=== Building Jellyfin.Plugin.PlaylistTriage (.NET 8 Release) ==="
dotnet publish "${PROJECT_DIR}/${PROJECT_DIR}.csproj" -c Release -o "${PROJECT_DIR}/bin/Release/net8.0/publish"

echo "=== Preparing Packaging Directory ==="
rm -rf "${OUTPUT_DIR}"
mkdir -p "${OUTPUT_DIR}/${PLUGIN_DIR_NAME}"

# Copy plugin binary
cp "${PROJECT_DIR}/bin/Release/net8.0/publish/${DLL_NAME}" "${OUTPUT_DIR}/${PLUGIN_DIR_NAME}/"

# Copy client script
cp "${PROJECT_DIR}/Web/triageClient.js" "${OUTPUT_DIR}/triageClient.js"

echo "=== Creating Deployment Zip Archive ==="
cd "${OUTPUT_DIR}"
zip -r "${PROJECT_DIR}.zip" "${PLUGIN_DIR_NAME}"
cd ..

# Compute SHA256 checksum
if command -v shasum &> /dev/null; then
    CHECKSUM=$(shasum -a 256 "${OUTPUT_DIR}/${PROJECT_DIR}.zip" | awk '{print $1}')
elif command -v sha256sum &> /dev/null; then
    CHECKSUM=$(sha256sum "${OUTPUT_DIR}/${PROJECT_DIR}.zip" | awk '{print $1}')
else
    CHECKSUM="N/A"
fi

echo "=============================================================================="
echo "Build and packaging completed successfully!"
echo "Zip package:      ${OUTPUT_DIR}/${PROJECT_DIR}.zip"
echo "Plugin folder:    ${OUTPUT_DIR}/${PLUGIN_DIR_NAME}/"
echo "Client script:    ${OUTPUT_DIR}/triageClient.js"
echo "SHA256 Checksum:  ${CHECKSUM}"
echo "=============================================================================="
