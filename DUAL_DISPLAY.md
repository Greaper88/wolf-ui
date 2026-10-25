# Dual-display launcher integration

This branch adds Wolf-UI's part of the negotiated dual-display workflow. It does
not implement video encoding, multiplexing, client decoding, or compositor output
creation. Those belong to Wolf and Moonlight. Wolf-UI enables video-mode selection
only when the owned session explicitly reports `dual_display.available: true`.

## Modes

- `off` and `controls` retain the existing single-app launch requests and do not
  send a display target.
- `dual_monitor` starts one app containing the complete extended desktop. Wolf
  owns applying the negotiated horizontal/vertical layout and primary display.
  Wolf-UI reports those settings and does not enter independent-app selection.
- `dual_app` asks which display should receive the first selected app. Once a
  session snapshot reports one running slot, the next app targets the remaining
  launcher slot automatically. With both slots running, the display picker
  explicitly identifies replacement targets.

The app picker uses the established persistent-app model: selecting an app that
already has a persistent lobby reconnects that existing app to the chosen display.
Starting two instances of the same persistent app is a separate lifecycle feature.

## Additive API contract

The current session returned by `GET /api/v1/sessions` can contain:

```json
{
  "dual_display": {
    "version": 1,
    "mode": "dual_app",
    "layout": "horizontal",
    "primary": 0,
    "available": true,
    "active_display": 1,
    "displays": [
      {"id": 0, "width": 1920, "height": 1080, "fps": 60, "x": 0, "y": 0},
      {"id": 1, "width": 1280, "height": 720, "fps": 30, "x": 1920, "y": 0}
    ],
    "slots": [
      {"display_id": 0, "app_id": "app-session-id", "state": "running"},
      {"display_id": 1, "app_id": null, "state": "launcher"}
    ]
  }
}
```

IDs 0 and 1 identify video tracks throughout the session; UI labels show Display 1
and Display 2. They are independent of Godot's local monitor indices. Unsupported
versions, unavailable video transport, invalid dimensions/IDs, and incomplete
dual-app slot snapshots block a video-mode launch with a clear message. Missing
`dual_display` retains compatibility with older servers.

`POST /api/v1/lobbies/create`, `/lobbies/join`, and `/runners/start` have an optional
`display_id`. App creation takes the chosen display's width, height, and frame
rate. Joining an existing or shared lobby also forwards the target, including
when the viewer has not selected a profile yet.

Wolf must atomically assign the app producer to that video track and keep or move
the launcher producer onto the remaining free track. Wolf-UI does not attempt to
simulate that by moving its local Godot window. Session status refreshes after a
successful join and every five seconds; the server's accepted slot state remains
authoritative.

## Validation

Run `bash tests/dual-display/run.sh /path/to/disposable/src` with Godot 4.4.1 Mono,
.NET 8, and Python 3. The fixture uses the actual UI scenes and a private mock Wolf
Unix socket. No Docker application launches, live streams, GPUs, or physical
second displays are involved. The existing running-app suite remains available
at `tests/running-app-status/run.sh`.

Real two-track video, launcher relocation, display hotplug, and remote input
routing still require cross-project hardware/integration validation.
