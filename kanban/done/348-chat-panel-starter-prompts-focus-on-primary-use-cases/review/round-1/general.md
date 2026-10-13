# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** fb80a7891..f5267b12e

## Summary
No bugs found. `XaiChatOptions.StarterPrompts` is a get-only list initialised empty (defaults are NOT pre-filled), so the configuration binder appends into an empty list and the "append vs replace" risk does not occur; the bind test covers it. Null/blank/failure paths all end at `AgentChatStarterPrompts.Defaults` (Normalize accepts null; failure branch resets state; the new field is reset in the state-reset method). Remaining items are minor coverage and accuracy points.

## Issues
### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-starter-prompts-tests.cs (no server-handler test); source/container-apps/web/features/agent-chat/get-agent-chat-configuration/get-agent-chat-configuration-handler-application.cs:71
- Description: The server hop (`options.StarterPrompts` -> `Normalize` -> `Response.StarterPrompts`) is not exercised by any test. Tests cover Bind+Normalize in isolation and the SPA with a scripted server response, but nothing asserts the handler actually wires the option into the response (a dropped line or wrong argument would pass all tests).
- Suggestion: Add one test that runs the handler with `XaiChatOptions` holding a prompt and asserts the response carries it (and defaults when empty), if an existing handler-test harness is available.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/features/agent-chat/agent-chat-starter-prompt-contracts.cs:11
- Description: Design says "XAI:StarterPrompts replaces the whole list." The binder merges list entries by index across configuration providers (e.g. appsettings.json with 4 entries plus appsettings.Development.json with 2 overrides indices 0-1 field-by-field and keeps 2-3). So layered config does not strictly replace.
- Suggestion: Reword to "the configured list is used instead of the defaults" and optionally note that layered sources merge by index.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/features/agent-chat/agent-chat-starter-prompt-contracts.cs:44
- Description: `Defaults` is a shared static list of mutable records (`set` accessors) and `Normalize`, the response default, and `AgentSurfaceState` all hand out the same instances. Nothing mutates them today, but a consumer mutating a returned prompt would change the defaults process-wide.
- Suggestion: Optional: make the properties `init` (binder and System.Text.Json both support init) or accept as-is.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.load-chat-configuration.cs:16
- Description: The Design line exceeds the surrounding wrapped width (about 125 chars); style only.
- Suggestion: Re-wrap.
- Status: open
