# Experimental GPU-selection integration

Use this branch with the matching `gpu-selection-dev` branch of
[Greaper88/wolf](https://github.com/Greaper88/wolf/tree/gpu-selection-dev).
Build the UI with `docker build -t wolf-ui:gpu-selection-dev .`, then configure Wolf's
Wolf UI app runner to use that image. Updating only the Wolf server image is insufficient.

The footer displays the session GPU and available telemetry. Single-user app launches send
the source session and selected profile to Wolf. Persistent apps are matched by profile and
state folder; reconnecting from another device joins the existing app on its original GPU.
Join failures display the server explanation instead of treating every failure as a full lobby.

Known limits of the paired development builds:

- Automatic GPU routing uses VA hardware encoders, tested with Mesa on AMD. NVIDIA/NVENC
  automatic routing is not integrated.
- Single-user persistent apps are supported; simultaneous multi-user lobby sharing is not.
- Automatic pipelines use ordinary video buffers, not zero-copy.
- AMD encoder utilization is currently unknown in the selector. The footer's other-user count
  is based on stream-session counts, not unique human users or all resident applications.
- Older servers can omit GPU metadata; the footer then stays hidden. The new server requires
  the updated source-session/profile API fields for automatic sub-app routing.

The Docker image builds successfully, and paired live tests cover separate profile Steam
instances and reconnects. This is an experimental checkpoint; the initial commit skips the
inherited image-publishing CI workflow and does not represent a stable release.
