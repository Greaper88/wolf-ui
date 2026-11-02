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

The image `wolf-ui:gpu-footer-candidate` was used for initial local verification.
Both companion release images were subsequently built from committed source,
published and deployed together. The UI source revision is
`11218a6d9296484d1075dd5c8c3d38a62295255a`; tags are
`ghcr.io/greaper88/wolf-ui:gpu-selection-11218a6` and `gpu-selection-dev`, with
manifest digest `sha256:0010941ef47d511b7c2823f3ab55ca797216631a8b1f1b20c76fd91985bf194e`.
See the [paired publication record](https://github.com/Greaper88/wolf/blob/dev-gpu-selection/docs/modules/dev/pages/gpu-footer-publication-2026-09-25.md)
for server details, deployment checks and publication tools.

## Tools

Codex execution, patch, image-viewing and UTC clock tools; Git and ripgrep; Docker
build, exec and copy; .NET 8; Godot 4.4.1; Xvfb; temporary C#/GDScript fixtures for
formatting and scene rendering. The companion server used CMake/Ninja, the existing
compiler toolchain, clang-format 21 and Python API/stream checks. No subagents,
browser automation or external research were used. Preview values were examples,
not measurements from a user's live session.

## Preserve readings during failed polls: 2026-10-11

AI-Agent: ChatGPT Codex
AI-Model-Version: GPT-6.1 Sol Extra High (client-displayed label supplied by the maintainer)
AI-Inference-Date: 2026-10-11 02:31:13 UTC

The serving model variant cannot be independently verified from this workspace.

At the maintainer's request, temporary session-request failures now leave the
GPU footer's visibility, text, tooltip and usage readings unchanged. The next
valid session response updates the footer normally, including hiding it when
that response contains no GPU metadata. The pending-request guard still resets
after a failure. This display change does not fix the underlying request errors.

Verified with a .NET 8 build of the current dev source in a disposable directory
inside the cached UI builder: zero errors and 22 analyzer-loading warnings about
the unavailable `System.Collections.Immutable` 9.0 assembly. `git diff --check`
also passed. No new automated tests were added for this small display change.
The live images and running user apps were not changed or restarted.

Tools used: Codex execution, patch and UTC clock tools; Git, ripgrep and file
reads; Docker inspect, start, copy, exec and stop; and .NET restore/build with
cached NuGet packages. No subagents, browser automation or external research
were used for this change.

## API failure diagnosis: 2026-10-11

AI-Agent: ChatGPT Codex
AI-Model-Version: GPT-6.1 Sol Extra High (client-displayed label supplied by the maintainer)
AI-Inference-Date: 2026-10-11 03:01:36 UTC

The serving model variant cannot be independently verified from this workspace.

Live .NET EventPipe tracing identified a broken pipe while the client reused a
closed Wolf API connection. Wolf's companion fix explicitly marks ordinary
responses `Connection: close`, retaining SSE keep-alive. The UI now logs the
underlying exception type and message so future transport failures are actionable.
The requested preservation of footer readings remains in place.

The clean UI build, Godot import and Linux release export passed. Temporary
diagnostic scripts were removed before exporting. The local candidate image is
`wolf-ui:api-connection-fix-dev`; the live image and running apps remain unchanged.

Tools used: Codex execution, patch and UTC clock tools; Git and ripgrep; Docker;
.NET 8; Godot 4.4.1 Mono; Python 3; ILSpy 9.1.0.7988; Microsoft's diagnostics client
0.2.750501 and TraceEvent 3.2.8; and official Microsoft/.NET source documentation
accessed using the web tool. Companion server validation used CMake, Ninja,
Catch2 and clang-format 18.1.8. No subagents were used. The server's full evidence
and validation record is `docs/modules/dev/pages/ai-assistance-api-connection-close.adoc`.

The maintainer subsequently authorized the local rollout on 2026-10-10 (Chicago
time). The tested companion was applied at 2026-10-11 04:10 UTC under the existing
`wolf-ui:gpu-selection-dev` alias. Its next real client connection used image
`sha256:9d2ca74ffc51ce877fe1adc158337e2687b19144ef130a22f2518368bbedb539`
and showed no request failures in the checked window. A separate headless live-API
check also completed several polling cycles without transport failures. The prior
image was retained for rollback. No GHCR publication or source push was performed.
