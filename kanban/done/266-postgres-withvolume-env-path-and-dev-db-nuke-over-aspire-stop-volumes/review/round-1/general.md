# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** full branch diff (AppHost volume wiring, constants, aspire-tests model test,
dev db nuke command + helpers, shared version guard refactor, docs, template.json excludes)

## Summary

WithVolume(env:) keeps WithDataVolume's identity (name via VolumeNameGenerator, target
/var/lib/postgresql); the model test checks parity against Aspire itself, so it's a sound
upgrade guard. The version guard is extracted cleanly and `dev run` messages are unchanged.
Verified: `aspire stop --apphost <csproj>` exits 0 when no AppHost is running (Aspire 13.6.0),
so nuke after a manual stop still reaches the Docker sweep. Two small issues remain in nuke.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/endpoints/db-nuke-command.cs:150
- Description: The Design region says the sweep "never reaches beyond the list step 2 printed".
  But the `--yes` path never printed the list, and the sweep re-lists after the stop and removes
  every prefixed volume, including any created after the first listing.
- Suggestion: Print the resolved volumes in the `--yes` path before the stop. Sweep only the
  intersection with the volumes resolved before acting.
- Status: open

### Issue 2 — Severity: nit
- File: tools/dev-cli/services/db-nuke.cs:84
- Description: Sanitize uses `char.IsNumber`, which accepts non-ASCII digits. Aspire's
  VolumeNameGenerator accepts ASCII letters and digits only.
- Suggestion: Use `char.IsAsciiLetterOrDigit`.
- Status: open
