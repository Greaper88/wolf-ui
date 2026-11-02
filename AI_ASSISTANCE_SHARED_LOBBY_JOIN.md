# Shared lobby joins from the profile-selection page

AI-Agent: ChatGPT Codex
AI-Model-Version: GPT-6 family; exact serving version and reasoning setting unavailable
AI-Inference-Date: 2026-10-17 06:48:33 UTC

## Problem and change

The shared-lobby Join button is available before a viewer selects a profile.
The GPU-aware join request unconditionally dereferenced the active profile,
which is null on that page. Real UI logs showed a NullReferenceException in
Lobby.JoinLobby; no join request reached Wolf. This originated with the
persistent GPU-aware app-session integration, rather than the later reconnect
admission fix.

The request now includes the viewer's profile when selected and otherwise
sends a null profile. It never substitutes the lobby owner's identity. Wolf's
existing lobby PIN and private-app profile checks are unchanged.

Join and leave notifications now read lobby_id from JSON instead of slicing
text around commas. Optional profile and PIN fields, including a different
field order, therefore preserve the lobby's player-count updates.

## Validation

The existing Godot scene and private mock Unix-socket fixture now exercise the
front-page Join button before profile selection, selected-profile joining via
the actual PIN dialog, viewer identity and PIN forwarding, event IDs and player
counts with reordered or additional fields. The prior running-app status
checks remain in the suite. All fixture identities and PINs are synthetic.

The original source reproduced the reported NullReferenceException and failed
before an anonymous join could reach the mock server. Applying only the null
profile fix passed those checks but exposed the independent event-ID parsing
failure. Both fixes passed all 24 checks (10 sharing checks plus the original
14 running-app status checks). Builds reported zero warnings and errors.
The existing Godot resource-cleanup warnings at process exit were present in
both baseline and corrected runs; the corrected suite exited successfully.
Python fixture syntax and Git whitespace checks also passed.

The clean Linux ExportRelease build, Godot import and release export passed.
The exported executable reports Godot 4.4.1 Mono, and its release assembly does
not contain the temporary regression-test class. The candidate image is
`wolf-ui:lobby-join-fix-dev`, prepared from the existing runtime image. Once
validated, its `wolf-ui:gpu-selection-dev` alias applies only to newly created
UI containers. Live Wolf, existing app containers and streams are not
restarted. No source push or registry publication is part of this repair.
The local validation directory records image and rollout details.

## Tools

Codex execution, patching, UTC clock and collaboration tools; a parallel
read-only code review and test implementation; Git, Bash, ripgrep, Python 3,
Docker, .NET 8/C# and Godot 4.4.1 Mono. Read-only live container logs and safe
API fields were used for diagnosis. No external messaging or web research was
used. Pre-existing UI transport/footer edits were preserved.

## Local rollout

The tested replacement was applied at 2026-10-17T06:51:01.317818+00:00 under
`wolf-ui:gpu-selection-dev`. Its image ID is
`sha256:daea35773393e7a417e8731d2b214f75556593ccb646ecb0d60e9dcc660469b4`.
The prior image is retained as `wolf-ui:before-shared-lobby-join-20261017`.
Wolf's container ID, startup time and restart count were unchanged. Existing
UI instances continue on their original image; a new UI container is required
to receive the fix. The build container was returned to its stopped state.
The image's bundled disclosure records pre-rollout validation; this section
records the resulting deployment. No branch or remote image tag was changed.
