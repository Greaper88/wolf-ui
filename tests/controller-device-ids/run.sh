#!/usr/bin/env bash
set -euo pipefail

# Run against a disposable checkout with Godot Mono and .NET 8 available.
project_root=$(realpath "${1:?Pass the disposable src directory}")
test_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
test_scene="$project_root/ControllerDeviceIdTests"
test ! -e "$test_scene"
cleanup() { rm -rf -- "$test_scene"; }
trap cleanup EXIT
mkdir "$test_scene"
cp "$test_dir/ControllerDeviceIdChecks.cs" "$test_dir/ControllerDeviceIdChecks.tscn" "$test_scene/"
export GODOT_SILENCE_ROOT_WARNING=1
dotnet build "$project_root/Wolf-UI.csproj" -p:NuGetAudit=false --ignore-failed-sources
Godot --headless --path "$project_root" --editor --import --quit
timeout 30s Godot --headless --path "$project_root" res://ControllerDeviceIdTests/ControllerDeviceIdChecks.tscn
