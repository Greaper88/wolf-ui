# Manual GPU selection

Pair this branch with Wolf's `dev-manual-gpu-selection` branch. A GPU picker opens
before a new single-user or co-op app launch. Existing apps keep their GPU when
reconnected. The GPU button in the header opens a live status panel with names,
VRAM usage/total, GPU utilization, connected viewers and resident app counts.

The list disables GPUs that cannot encode the current Moonlight codec. Selection
uses the stable GPU ID returned by Wolf; the backend validates it again at launch
and when another viewer joins. Choosing the configured GPU preserves the legacy
launch path. GPU API failures offer that path explicitly instead of silently
choosing another device. Missing metrics display as unavailable.

The backend probes AMD/Intel VA and NVIDIA NVENC SDR encoders and requires a
GPU-memory pipeline for explicit selection. NVIDIA choices bind the encoder and
CUDA context to the chosen physical GPU. Hardware validation remains necessary;
factory availability alone is not proof of end-to-end zero-copy operation.
The five-second polling interval is informational and never drives placement.

Validation: Debug build, Linux release export and automated picker/status checks
passed. The developer reports that the panel refreshes, an app launches for one
user, and Steam identifies the selected GPU. Multi-user counts and returning to
the launcher remain unverified in the live session.
