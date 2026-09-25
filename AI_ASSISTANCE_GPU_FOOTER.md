# GPU bottom bar: 2026-09-25

AI-Agent: ChatGPT Codex
AI-Model-Version: GPT-6 Astra Medium
AI-Inference-Date: 2026-09-25 03:09:20 UTC

The model designation is the maintainer-supplied session label.

## Change

Moved GPU status into the existing bottom bar, left-aligned with action hints kept
on the right. It shows the human-readable GPU name, short render node, stream
codec, GPU usage, VRAM capacity and **Other users on GPU**. PCI identifiers and
encoder usage are no longer shown. Long text truncates with the full text available
on hover. Missing usage or codec information is shown as unknown.

Older servers can omit the new fields without breaking the UI. Legacy names
containing PCI identifiers use a generic GPU label. Servers without GPU metadata
keep the status hidden. The other-user count still means stream-session count
minus the current session, not unique profiles.

## Verification

- .NET build: zero warnings and zero errors.
- Godot editor import and Linux export succeeded.
- Rendered the actual footer scene under Xvfb at ordinary and narrow widths;
  checked left alignment, ellipsis and no overlap with the action hints.
- Invoked the compiled status formatter with current and older-server metadata.
- `git diff --check` passed.
- The companion Wolf change passed five standalone GPU suites and 69 assertions
  across six API/serialization cases.
- Disposable streams on the RX 7600 and Radeon Pro WX 4100 reported correct names,
  HEVC and GPU usage. Cross-device persistent-app handoff and return to the
  original launcher GPU passed with continuing video frames.

The image `wolf-ui:gpu-footer-candidate` is a local verification artifact. This
change has not been published or deployed. Both companion images need updating to
provide the new metadata and display together.

## Tools

Codex execution, patch, image-viewing and UTC clock tools; Git and ripgrep; Docker
build, exec and copy; .NET 8; Godot 4.4.1; Xvfb; temporary C#/GDScript fixtures for
formatting and scene rendering. The companion server used CMake/Ninja, the existing
compiler toolchain, clang-format 21 and Python API/stream checks. No subagents,
browser automation or external research were used. Preview values were examples,
not measurements from a user's live session.
