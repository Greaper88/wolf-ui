#!/usr/bin/env bash
set -euo pipefail

# Use a disposable checkout with Godot 4.4.1 Mono, .NET 8 and Python 3.
project_root=$(realpath "${1:?Pass the disposable src directory}")
test_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
test_scene="$project_root/DualDisplayTests"
test_fixture=$(mktemp -d /tmp/wolf-ui-dual-display.XXXXXX)
test_server_pid=
test ! -e "$test_scene"
cleanup() {
    if [[ -n "$test_server_pid" ]]; then
        kill "$test_server_pid" 2>/dev/null || true
        wait "$test_server_pid" 2>/dev/null || true
    fi
    rm -rf -- "$test_scene" "$test_fixture"
}
trap cleanup EXIT
mkdir "$test_scene"
cp "$test_dir/DualDisplayChecks.cs" "$test_dir/DualDisplayChecks.tscn" "$test_scene/"
printf '{"lobbies":[]}' > "$test_fixture/state.json"
python3 "$test_dir/../running-app-status/mock_wolf.py" "$test_fixture" &
test_server_pid=$!
export WOLF_UI_TEST_FIXTURE="$test_fixture" WOLF_SOCKET_PATH="$test_fixture/wolf.sock"
export WOLF_SESSION_ID=dual-display-fixture WOLF_UI_AUTOUPDATE=False GODOT_SILENCE_ROOT_WARNING=1
dotnet build "$project_root/Wolf-UI.csproj" -p:NuGetAudit=false --ignore-failed-sources
Godot --headless --path "$project_root" --editor --import --quit
timeout 90s Godot --headless --path "$project_root" res://DualDisplayTests/DualDisplayChecks.tscn
