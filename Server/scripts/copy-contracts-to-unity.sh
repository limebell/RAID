#!/bin/zsh
set -e

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT_DIR/src/Raid.Contracts/Raid.Contracts.csproj"
OUTPUT_DLL="$ROOT_DIR/src/Raid.Contracts/bin/Debug/netstandard2.1/Raid.Contracts.dll"
UNITY_PLUGINS_DIR="$ROOT_DIR/../Client/RAID/Assets/Plugins"

dotnet build "$PROJECT" -c Debug --nologo
mkdir -p "$UNITY_PLUGINS_DIR"
cp "$OUTPUT_DLL" "$UNITY_PLUGINS_DIR/Raid.Contracts.dll"
