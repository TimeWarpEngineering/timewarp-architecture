# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general (single pass by the orchestrator over the fix delta `df2f61ddf` vs `9df34f363`)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 1 | 4 | 0 |

## Resolved prior (round 1)

Evidence for each is in `general.md` under Round-1 re-verification.

| ID | Severity | Status |
|----|----------|--------|
| M1 | bug | fixed |
| M2 | bug | fixed |
| M3 | bug | fixed |
| M4 | bug | fixed |
| M5 | suggestion | fixed |
| M6 | suggestion | fixed |
| M7 | suggestion | fixed |
| M8 | suggestion | fixed |
| M9 | nit | fixed |
| M10 | nit | fixed |
| M11 | nit | fixed |
| M12 | nit | fixed |

## Issues

### M13 — Severity: nit — Status: open
- File: source/container-apps/web/features/agent-chat/xai-chat-contracts.cs:82
- Description: `XaiChatDefaults.SetupHint(bool development, bool adminStorage = false)` has no production caller after the `HostSetupText` / `AdminHint` split. Only `get-agent-chat-configuration-tests.cs:48`, `:51`, `:58`, and `:64` call it. It is a public member on a shipped contracts type, and it rebuilds the "command + Admin sentence" string that M2 and M3 removed.
- Suggestion: delete it. Point the test lines at `HostSetupText` and the handler's `AdminHint`, or drop the duplicate assertions.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.

## Evaluate

One open finding remains (M13, nit), so there is no disposition yet. A fix pass is needed on this task id. Round 3 re-verifies M13.
