# Running-app state regression checks

Run with Godot 4.4.1 Mono, .NET 8 and Python 3 in a disposable UI build checkout
whose assets are imported and NuGet packages restored:

```sh
bash tests/running-app-status/run.sh /path/to/disposable/wolf-ui/src
```

The harness opens the real UI scenes against a private fake Wolf Unix socket. It
does not access Docker, real profiles, GPUs or running app containers. Temporary
Godot test scripts and fixture state are removed when the run ends. Rebuild after
running tests before exporting an image so the test class is not in the assembly.

Coverage includes live `profile_id` events, reconnect snapshots with different GPU
metadata, missed events, failed/stale refreshes, profile ownership, a reused app ID
returned without a create event, and successful/failed Stop requests. The original
code fails the live-event check before the periodic refresh can hide the failure.

Shared-lobby coverage first presses the real front-page Join button without
selecting a profile and verifies that the request reaches the fixture without
claiming the owner's identity. It then checks selected-profile joins through the
real PIN dialog, request identity/PIN forwarding, and join/leave event IDs and
player counts with extra or reordered fields. All PINs and profiles in these
checks are synthetic fixture values.
