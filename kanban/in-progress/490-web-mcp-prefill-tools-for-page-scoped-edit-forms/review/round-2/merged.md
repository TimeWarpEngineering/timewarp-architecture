# Round 2 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/profiles/pages/ProfilePage.razor:84
- Description: A suggestion that arrived during the profile fetch was applied onto the empty command and then lost.
- Disposition notes: Fixed in aa25cba8c. The `WorkingCopyReady` flag is set only after a loaded profile is copied. Regression test: `ProfilePage_Should_.Keep_A_Draft_That_Arrives_While_The_Profile_Is_Loading`.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/profiles/profile-page-prefill-tests.cs
- Description: No page-lifecycle coverage for the dirty check, confirm, apply, clear, and badge sequence.
- Disposition notes: Fixed in aa25cba8c. Five page tests on an interactive HTML host; Results record 5 passed. The browser smoke was not run. Results name the page tests as the substitute.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/form-prefill.values.cs:193
- Description: Empty arguments, or arguments with only unknown keys, returned `drafted: true`.
- Disposition notes: Fixed in aa25cba8c. `NoMatch` returns false with the sorted accepted keys, and nothing is stored. Covered on the WebMCP and AIFunction paths.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/profiles/profile-state/profile-state.confirm-profile-suggestion.cs:39
- Description: The confirm text said "replaces what you typed" after an agent-only draft.
- Disposition notes: Fixed in aa25cba8c. The text now says "replaces unsaved changes on this form", and a test asserts it.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.prefill.cs:47
- Description: An unreachable not-page-bound branch.
- Disposition notes: Fixed in aa25cba8c. The branch is removed. The Design region states that Prefill tools are page-bound by construction, which form-prefill.cs:93 confirms.

## Duplicates / conflicts

- None. Round 2 raised no new findings.
