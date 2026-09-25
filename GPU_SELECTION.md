# Experimental GPU-selection integration

Use this branch with the matching `dev-gpu-selection` branch of
[Greaper88/wolf](https://github.com/Greaper88/wolf/tree/dev-gpu-selection).
Build the UI with `docker build -t wolf-ui:gpu-selection-dev .`, then configure Wolf's
Wolf UI app runner to use that image. Updating only the Wolf server image is insufficient.

The existing bottom bar displays the session GPU's human-readable name, short render node,
negotiated codec, GPU usage, VRAM capacity and other users on the GPU. Status text is
left-aligned; Confirm/Back remain on the right. Long text truncates with the full status
available on hover. PCI addresses and encoder utilization are not displayed. Missing
telemetry or codecs remain unknown; the UI never substitutes encoder load for GPU usage.
The companion server supplies `gpu_percent` and `codec` and resolves device names.

Single-user app launches send
the source session and selected profile to Wolf. Persistent apps are matched by profile and
state folder; reconnecting from another device joins the existing app on its original GPU.
Join failures display the server explanation instead of treating every failure as a full lobby.

Known limits of the paired development builds:

- Automatic GPU routing uses VA hardware encoders, tested with Mesa on AMD. NVIDIA/NVENC
  automatic routing is not integrated.
- Single-user persistent apps are supported; simultaneous multi-user lobby sharing is not.
- Automatic pipelines use verified zero-copy where available, otherwise CPU-buffer
  conversion with hardware encoding (unless the admin requires zero-copy).
- AMD encoder utilization is currently unknown in the selector. The footer's other-user count
  is based on stream-session counts, not unique human users or all resident applications.
- Older servers can omit GPU metadata; the footer then stays hidden. The new server requires
  the updated source-session/profile API fields for automatic sub-app routing.

The Docker image builds successfully, and paired live tests cover separate profile Steam
instances and reconnects. This is an experimental checkpoint; the initial commit skips the
inherited image-publishing CI workflow and does not represent a stable release.
