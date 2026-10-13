# Round 2 — merged findings
**Date:** 2026-10-13
**Sources:** general (re-review of fix commit 4181c4291)

## Counts (final, including carried round-1 IDs)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 5 | 1 |
| nit | 0 | 4 | 3 |

## Resolved prior

M1, M2, M4, M5, M6, M7, M8, M9 verified fixed. M3, M10 wontfix (see round-1/merged.md); round 2 confirms the fixes did not make them worse.

## Issues

### M11 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/resource-reference-type.cs
- Description: A 2xx literal `null` body raises InvalidOperationException from HttpApiService.ReadFromJson, uncaught by the search; picker could stay on "Searching…".
- Source: general (round-2 Issue 1)
- Disposition notes: InvalidOperationException now returns a failed search; Design region updated. `dev build` 0/0; AskReference SPA tests 9/9.

### M12 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor
- Description: Word-start check runs after JS round trips; a fast follow-up keystroke can keep the picker from opening.
- Source: general (round-2 Issue 2)
- Disposition notes: WASM-interactive Ask makes the window small, Playwright covers the normal path, and the user can retype `@`. Trade-off chosen over the round-1 focus-stealing defect (M6). Decided by: review orchestrator.

### M13 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/ask-reference-state/ask-reference-state.open.cs
- Description: Open does not guard against a principal change mid-flight.
- Source: general (round-2 Issue 3)
- Disposition notes: Very small window. The type list holds only type names filtered by the previous principal's policies; the server re-authorizes every search, so no data crosses users. Decided by: review orchestrator.

## Duplicates / conflicts

- None.
