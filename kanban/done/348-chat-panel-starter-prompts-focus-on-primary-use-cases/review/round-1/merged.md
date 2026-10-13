# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/agent-chat/get-agent-chat-configuration/get-agent-chat-configuration-handler-application.cs:71
- Description: No test covers the server handler passing `options.StarterPrompts` through `Normalize` into the response.
- Suggestion: Add a handler test with a configured prompt and with an empty list.
- Source: general
- Disposition notes: Added `Send_Normalized_Configured_Prompts_From_The_Handler` to `ask-starter-prompts-tests.cs`. It runs the real handler with fake principal and upstream ports and asserts the configured list (trimmed) and the defaults fallback.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/agent-chat/agent-chat-starter-prompt-contracts.cs:10
- Description: The Design region says "replaces the whole list", but layered configuration sources merge list entries by index.
- Suggestion: Reword it.
- Source: general
- Disposition notes: The Design region now says the configured list is used instead of the defaults, and that layered sources merge by index.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/agent-chat/agent-chat-starter-prompt-contracts.cs:24
- Description: The shared static `Defaults` hands out records with `set` accessors.
- Suggestion: Make the properties `init`.
- Source: general
- Disposition notes: Changed the properties to `init`. The binder test and System.Text.Json round-trip still pass.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.load-chat-configuration.cs:16
- Description: A Design line is longer than the surrounding wrap width.
- Suggestion: Re-wrap it.
- Source: general
- Disposition notes: Re-wrapped.

## Duplicates / conflicts

- None (single reviewer).
