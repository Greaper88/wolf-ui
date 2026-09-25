# GPU-selection integration

Use this primary `auto-gpu-selection` branch with the matching `auto-gpu-selection` branch of
[Greaper88/wolf](https://github.com/Greaper88/wolf/tree/auto-gpu-selection).
Published images are `ghcr.io/greaper88/wolf:latest` and
`ghcr.io/greaper88/wolf-ui:latest`. Set the server image in Compose and the UI image
in Wolf's `config.toml` app runner, and pull both images explicitly. Existing
configurations retain their saved UI image. Updating only the Wolf server image is insufficient.
For a local build, use `docker build -t wolf-ui:latest .` and point the UI runner
at that local tag.

Future NVIDIA and vGPU work continues on `dev-gpu-selection`, using the paired
`gpu-selection-dev` image tags. Keep server and UI on the same channel.

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

Current limitations:

- Automatic GPU routing uses VA hardware encoders, tested with Mesa on AMD. NVIDIA/NVENC
  automatic routing is not integrated.
- Single-user persistent apps are supported; simultaneous multi-user lobby sharing is not.
- Automatic pipelines use verified zero-copy where available, otherwise CPU-buffer
  conversion with hardware encoding (unless the admin requires zero-copy).
- AMD encoder utilization is currently unknown in the selector. The footer's other-user count
  is based on stream-session counts, not unique human users or all resident applications.
- Older servers can omit GPU metadata; the footer then stays hidden. The new server requires
  the updated source-session/profile API fields for automatic sub-app routing.

Paired tests cover separate profile Steam instances, reconnects, cross-device persistent
apps, GPU metadata and the footer layout. Promotion to the primary branch preserves the
current VA/Mesa feature scope; automatic NVIDIA routing and vGPU management are future work.
