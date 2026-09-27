# Running-app status restoration

AI-Agent: ChatGPT Codex
AI-Model-Version: GPT-6 Astra Medium
AI-Inference-Date: 2026-09-27 01:15:11 UTC

The model designation is the maintainer-supplied session label.

## Change

Persistent apps could reconnect through Start while their tile lacked the running
badge and Stop action. The live create event identifies its owner with `profile_id`,
while the running-app snapshot uses `started_by_profile_id`. App tiles now accept
both, matching the selected profile and persistent runner folder independently of
GPU assignment.

The app list reconciles against the server when opened and every five seconds while
visible. Failed requests preserve known running apps; successful empty snapshots
clear them. Older snapshots cannot overwrite newer lifecycle events or a rebuilt
profile view. Rebuild results from a previous profile are discarded.

The tile retains the app ID returned by CreateLobby even when Wolf reused a
persistent app without emitting a create event. This also preserves Stop if joining
fails, for example because the pinned GPU cannot encode the negotiated codec.
Running apps take precedence over local image availability. Stop now clears the
tile only after a successful response and retains it with an error dialog on failure.
Removed tiles unsubscribe from image events and stop scheduling image checks.

Both maintained branches receive the same changes. GPU selection, pinning and
encoder admission are unchanged.

## Validation

- .NET 8 build succeeded with zero warnings and errors after the final changes.
- The real Godot 4.4.1 UI scenes passed 14 checks against an isolated fake Wolf
  Unix socket. Coverage includes live create events, reconnect snapshots with
  either GPU identity, missed events, failed/stale snapshots, profile isolation,
  reused app IDs, and successful/failed Stop requests targeting the correct ID.
- The original source failed the live create-event check using the same fixture.
- Both old and fixed UI runs reported existing Godot object/resource cleanup
  warnings at process exit; the behavioral assertions completed successfully on
  the fixed version. The fixture does not use real GPUs or stop real apps.
- Live server metadata was inspected read-only to verify profile ownership and
  persistent runner folder names.

## Tools

Codex execution, patch and UTC clock tools; Git, Bash, ripgrep, Python 3 and standard
file utilities; Docker build/copy/exec; .NET 8, the C# compiler and Godot 4.4.1 Mono.
The regression fixture uses a private Python Unix-socket HTTP server and real
Godot scenes. No subagents or external messaging were used.

## Local deployment: 2026-09-27 01:23 UTC

The user authorized restarting the dev stack after validation. Both images were
built with the repository Dockerfiles, including the pending Wolf codec guidance.
Godot's Linux release export and the exported executable's version check passed.
An isolated Wolf startup verified the API, codec message and existing GPU capability
results before deployment. The temporary smoke-test container was removed.

- `wolf:gpu-selection-dev`:
  `sha256:6810e424c2effd665a5fd8d02b2ad82e3a100faf52a1b8962871b036c44e5207`
- `wolf-ui:gpu-selection-dev`:
  `sha256:e203cd2a75a6693bf4fff5a867061102f7c56b73ad2b7bde5e3724cdb982c3a3`

The main Compose file uses GHCR with `pull_policy: always`. A new
`/root/wolf/docker-compose.override.yml` selects the local Wolf dev image with
`pull_policy: never`, so normal Compose restarts retain the local fix. The main
file was not modified. The saved app configuration already selected the local
Wolf-UI dev image. Remove that override to return to the published server image.

Rollback images are tagged `wolf:before-running-app-status-20260927` and
`wolf-ui:before-running-app-status-20260927`. Wolf was stopped with a 45-second
grace period; the previously identified unresponsive app container was no longer
running afterward. The replacement server, WolfDen and Moonlight HTTP endpoint
passed health checks with zero server restarts. Startup retained RX 7600 zero-copy
H.264/HEVC/AV1 and WX4100 hardware H.264/HEVC with CPU-buffer fallback.

At this deployment checkpoint, these were local working-tree builds. Source changes
were mirrored to both maintained branches but had not been committed or pushed;
GHCR tags had not been published. The maintainer subsequently confirmed the live
running indicator and the Connect/Stop controls were restored. No native Moonlight
interaction latency measurement is claimed.
