# Wolf-UI
A UI for Wolf, the main entrypoint when starting a streaming session 

This fork's `auto-gpu-selection` branch is the companion to the automatic GPU-selection
[Wolf fork](https://github.com/Greaper88/wolf). Use the matching primary images:

- Server: `ghcr.io/greaper88/wolf:latest`
- UI: `ghcr.io/greaper88/wolf-ui:latest`

Set the UI image in Wolf's `config.toml` app runner. Existing configurations keep
their saved image, so update that entry when migrating from upstream or the
`gpu-selection-dev` images. Pull the UI image explicitly; Wolf starts this child
container, so Compose does not pull it with the server. Stop and relaunch existing
Wolf-UI sessions after updating.

The `dev-gpu-selection` branches and paired `gpu-selection-dev` image tags remain
available for future NVIDIA and vGPU development. Use both images from the same
channel. See [GPU_SELECTION.md](GPU_SELECTION.md) for behavior and current limits.

----
### Supported Environment Args
- WOLF_UI_ARGS 

Append arguments to the Wolf-UI launch command.
To force Wolf-UI to use the OpenGl3 backend use `WOLF_UI_ARGS=--rendering-method gl_compatibility --rendering-driver opengl3`

- WOLF_SOCKET_PATH 

Set the path where Wolf-UI will look for the socket

- LOGLEVEL

Set the loglevel, valid arguments: `NONE` `ERROR` `WARNING` `WARN` `INFORMATION` `INFO` `DEBUG`

- WOLF_UI_AUTOUPDATE

if set to `True` then Wolf-UI will ask for a pull of the latest Wolf-UI image on start. Still WIP for none stable releases

---
### Special thanks to: 
- [THOSE AWESOME GUYS](https://thoseawesomeguys.com/) for their awsome [icon pack](https://thoseawesomeguys.com/prompts/)
