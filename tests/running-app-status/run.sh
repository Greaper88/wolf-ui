#!/usr/bin/env bash
set -euo pipefail

# Use a disposable checkout with Godot 4.4.1 Mono, .NET 8 and Python 3 installed.
project_root=$(realpath "${1:?Pass the disposable src directory}")
test_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
test_scene="$project_root/RegressionTests"
test_fixture=$(mktemp -d /tmp/wolf-ui-running-app.XXXXXX)
test_server_pid=
test -d "$project_root/.godot"
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
cp "$test_dir/RunningAppStatusChecks.cs" "$test_dir/RunningAppStatusChecks.tscn" "$test_scene/"
printf '{"lobbies":[]}' > "$test_fixture/state.json"
python3 "$test_dir/mock_wolf.py" "$test_fixture" &
test_server_pid=$!
export WOLF_UI_TEST_FIXTURE="$test_fixture" WOLF_SOCKET_PATH="$test_fixture/wolf.sock"
export WOLF_SESSION_ID=running-app-fixture WOLF_UI_AUTOUPDATE=False
dotnet build "$project_root/Wolf-UI.csproj" --no-restore
Godot --headless --path "$project_root" --editor --import --quit
timeout 90s Godot --headless --path "$project_root" res://RegressionTests/RunningAppStatusChecks.tscn
