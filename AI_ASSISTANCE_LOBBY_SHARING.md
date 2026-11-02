# Automatic GPU co-op lobby integration

AI-Agent: ChatGPT Codex
AI-Model-Version: GPT-6 Astra Medium
AI-Inference-Date: 2026-10-04 00:32:05 UTC

The model designation is the maintainer-provided session label; the exact serving
variant is not independently exposed. Prepared October 3, 2026, America/Chicago.
Human review remains pending.

Co-op launches use the current session's GPU metadata, send their source-session
identity, retain the actual created/reused lobby ID, and display join failures.
An unsuccessful join does not hide the still-running app or allow another co-op
launch against the same profile data. Re-entering a PIN-protected shared app from
the owner's tile asks for the lobby PIN. A missing join response now displays an
error from the lobby list as well.

The companion Wolf implementation owns encoding groups independently of viewers,
reuses compatible encoders, and starts another on the app's pinned GPU when
settings differ and capacity permits. Each encoder stops after its last viewer
leaves; the persistent app remains running. These are server responsibilities,
not UI lifetime management.

Validation: .NET compilation and 17 real Godot scene checks against a private mock
Wolf Unix socket passed. The added checks cover co-op source-session fields,
visible admission refusals, retained running state, and duplicate-launch
prevention. Existing analyzer and headless-exit resource warnings remain.
No live Moonlight co-op session was tested. During source validation, no running
service was restarted, image published, or Git commit/push performed. Only the primary
`auto-gpu-selection` worktree was changed.

## Local deployment: October 3, 2026

AI-Agent: ChatGPT Codex
AI-Model-Version: GPT-6 Astra Medium (maintainer-provided session label)
AI-Inference-Date: 2026-10-04 00:49:14 UTC

The maintainer requested local deployment after taking the stack down. A clean
release export, rebuilt after removal of the test harness, was packaged over the
previous validated UI runtime. The candidate is `wolf-ui:lobby-sharing-20261003`,
also selected by the existing `wolf-ui:gpu-selection-dev` alias; its image ID is
`sha256:02b5cf0feda1cf6d7845ef301d75f0988680395204072979e5c965c80bd88849`.
The previous alias is preserved as `wolf-ui:before-lobby-sharing-20261003`.

The executable starts inside the final image and reports Godot 4.4.1 Mono. The
matching Wolf stack is running, and its live app configuration selects this UI
alias. Actual UI launch and multiplayer/controller behavior await a Moonlight
client connection. No registry publication or Git commit/push was performed.

Additional tools: Docker/BuildKit build, tag, inspect, run, copy, and stop; Godot
release export and .NET release build; SHA-256 checks; and the companion server's
Docker Compose, runtime API, and startup-log checks. The server disclosure has
the complete local deployment record. Build containers were stopped afterwards.

Tools: Codex execution, patch and UTC clock tools; Git, Bash, ripgrep, Python,
Docker copy/exec, .NET 8, and Godot 4.4.1 Mono. No subagents were used.
The Wolf repository's `docs/modules/dev/pages/ai-assistance-lobby-sharing.md`
records server validation and the manual multiplayer acceptance checks.

## First-trial repair

AI-Agent: ChatGPT Codex
AI-Model-Version: GPT-6 Astra Medium (maintainer-provided session label)
AI-Inference-Date: 2026-10-04 04:06:00 UTC

Front-page lobby joining now permits a missing active profile rather than
throwing a null-reference exception. The server still checks private ownership
and any lobby PIN. The lobby list periodically reconciles successful API
snapshots, preserves entries on failed/stale snapshots, deduplicates create
events, updates membership counts, and releases subscriptions on removal.
Join, leave, and stop event IDs are read as JSON independently of field order.

Twenty-six real Godot scene checks passed, including front-page joining without
a profile, missed create/stop recovery, failed snapshots, duplicate entries,
membership field order, and removal cleanup. A clean release export followed
test cleanup. The image `wolf-ui:lobby-sharing-repair-20261003`, also tagged
`wolf-ui:gpu-selection-dev`, has ID
`sha256:5b628fe6eae5af3e1738e16ce9a7754f6fbcb206cfa8e238276f9e415e7f9632`.
The prior image remains under `wolf-ui:before-lobby-sharing-repair-20261003`.

Matching Wolf repairs were deployed with the maintainer's restart authorization.
Old test apps stopped and sessions/lobbies were empty afterwards. Software and
hardware video regressions passed, but a real Moonlight multiplayer retry is
still needed to confirm the reported first-frame freeze is resolved. No Git
commit/push or image publication was performed. Tools and deployment details
are in the companion server disclosure; no subagents were used.
