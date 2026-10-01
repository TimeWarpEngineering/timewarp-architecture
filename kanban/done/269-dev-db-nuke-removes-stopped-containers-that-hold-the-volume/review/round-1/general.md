# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** as framework, plus the unchanged surrounding handler flow (`Handle`, `StopAppHostAsync`, `SweepRemainingVolumesAsync`)

## Summary

The change adds pure helpers that list, parse, and plan container cleanup. It wires them into the dry listing and into the `--yes` sweep, which runs after `aspire stop`. The logic is correct:
- Containers are found per volume with `docker ps --all --filter volume=<name>`. Docker matches the volume name exactly, so the scope stays limited to this checkout's prefixed volumes.
- A running, paused, restarting, or removing container blocks the whole nuke.
- Removal is plain `docker rm`, never `-f`.
- If Docker is unreachable, the command stops without acting.

The tests cover every case the spec requires. The only problems are in user-facing messages: several claim "nothing removed" or "adopted" when the run has already got past `aspire stop`.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/services/db-nuke.cs:89
- Description: The running-container refusal says "Nothing was removed", but it is printed after `aspire stop --force --volumes` has already run. By then the AppHost is stopped, and Aspire may have deleted the volumes it owns. The operator would be told nothing happened when data may already be gone.
- Suggestion: Make the claim narrower: "No container or remaining volume was removed".
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/endpoints/db-nuke-command.cs:217
- Description: When `docker rm` fails, the message says "No volume was removed". This is false for the same reason: `aspire stop --volumes` has already run.
- Suggestion: "The remaining volume(s) were not removed".
- Status: open

### Issue 3 — Severity: nit
- File: tools/dev-cli/endpoints/db-nuke-command.cs:168
- Description: The sweep message "volume(s) Aspire adopted but does not own" is now inaccurate in the incident this task fixes. An Aspire-owned volume can be left behind because a stopped container held it while `aspire stop --volumes` ran.
- Suggestion: "volume(s) `aspire stop` left behind".
- Status: open

### Issue 4 — Severity: nit
- File: tools/dev-cli/endpoints/db-nuke-command.cs:29
- Description: The "Pure argument/listing/…" line in the Design region is over-long, with odd wrapping after the edit.
- Suggestion: Rewrap.
- Status: open
