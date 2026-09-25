# Wolf-UI
A UI for Wolf, the main entrypoint when starting a streaming session 

This is the `dev-gpu-selection` branch for future NVIDIA and vGPU work. Use
`ghcr.io/greaper88/wolf:gpu-selection-dev` with `ghcr.io/greaper88/wolf-ui:gpu-selection-dev`.
The primary branch is [`auto-gpu-selection`](https://github.com/Greaper88/wolf-ui/tree/auto-gpu-selection),
using `ghcr.io/greaper88/wolf:latest` and `ghcr.io/greaper88/wolf-ui:latest`.
Set the UI image in Wolf's `config.toml` app runner and pull both images when changing channels.

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