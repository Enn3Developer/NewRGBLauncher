#!/bin/bash

# Find the absolute path of the script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check if version parameter is provided
if [ "$#" -ne 2 ]; then
    echo "Version number is required."
    echo "Usage: ./build.sh [version]"
    exit 1
fi

BUILD_VERSION="$1"
RELEASE_DIR="$SCRIPT_DIR/releases"
PUBLISH_DIR="$SCRIPT_DIR/publish"

echo ""
echo "Compiling NewRGB with dotnet..."
dotnet publish -c Release --self-contained -r linux-x64 -o "$PUBLISH_DIR"

echo ""
echo "Downloading latest release"
vpk download github --repoUrl "https://github.com/Enn3Developer/NewRGBLauncher"

echo ""
echo "Building Velopack Release v$BUILD_VERSION"
vpk pack -u NewRGB -v "$BUILD_VERSION" -o "$RELEASE_DIR" -p "$PUBLISH_DIR"

echo ""
echo "Uploading..."
vpk upload github --repoUrl "https://github.com/Enn3Developer/NewRGBLauncher" --merge --releaseName "v$1" --tag "$1" --token "$2" -o "$RELEASE_DIR"