# Round 2 — general
**Date:** 2026-10-13
**Scope reviewed:** uncommitted working-tree diff over f5267b12e (fixes for M1–M4)

## Summary
All four round-1 items are fixed as described. The fix delta introduces no new defects: `init` setters remain compatible with the configuration binder and System.Text.Json, and all object-initializer call sites still compile (build gated clean).

## Issues
### Issue 1 (M1) — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-starter-prompts-tests.cs:179
- Description: Handler hop untested. Now `Send_Normalized_Configured_Prompts_From_The_Handler` runs the real handler with a configured option (asserts trim and carry-through) and an unset option (asserts Defaults). Fakes are minimal and fail loudly if completion is called.
- Suggestion: none.
- Status: fixed (verified)

### Issue 2 (M2) — Severity: nit
- File: source/container-apps/web/features/agent-chat/agent-chat-starter-prompt-contracts.cs:10
- Description: Design wording now says configured list is used instead of Defaults, notes layered sources merge by index. Accurate.
- Suggestion: none.
- Status: fixed (verified)

### Issue 3 (M3) — Severity: nit
- File: source/container-apps/web/features/agent-chat/agent-chat-starter-prompt-contracts.cs:26
- Description: Properties are `init`; Design region notes why. Binder and JSON deserialization support init; no remaining mutation sites found in the diff.
- Suggestion: none.
- Status: fixed (verified)

### Issue 4 (M4) — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.load-chat-configuration.cs:16
- Description: Line re-wrapped; content unchanged.
- Suggestion: none.
- Status: fixed (verified)

## New issues
None.
