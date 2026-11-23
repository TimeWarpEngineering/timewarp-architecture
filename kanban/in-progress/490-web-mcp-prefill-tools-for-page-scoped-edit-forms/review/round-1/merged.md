# Round 1 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/profiles/pages/ProfilePage.razor:78
- Description: A suggestion that arrives while `OnInitializedAsync` is still awaiting `FetchProfileData()` is applied onto the empty `new()` command (not dirty, so no prompt) and cleared. Then `CopyCommandFromProfile()` overwrites `Command` and clears `AgentFilled`. The draft is lost, and the tool has already returned `drafted: true`. The off-page offer → navigate → immediate Prefill flow makes this likely.
- Suggestion: Do not consume the suggestion until the working copy has been copied from a loaded `Profile`, using a page flag set in `CopyCommandFromProfile`. Add a regression test.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/profiles/pages/ProfilePage.razor:78
- Description: No test covers the page's dirty check → confirm → apply → clear → badge sequence, and the How to validate smoke was not run (no browser). "A dirty form asks before apply" is proven only at the action level.
- Suggestion: Add page-lifecycle coverage (clean applies without a prompt, dirty prompts, decline keeps the typed value, the Profile field badge, and the M1 regression). If no lane can drive it, run the smoke and record the result under `## Results`.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent/form-prefill.values.cs:30
- Description: Empty arguments, or arguments whose keys all go unrecognized, normalize to `"{}"`. The tool stores that and returns `drafted: true` with `fields: []`, so the agent gets a false success. On a dirty form the person also sees a pointless "Replace unsaved edits?" dialog.
- Suggestion: When no field matched, return `drafted: false` with an error that lists the accepted keys, and store nothing. Update the test that asserts the empty case succeeds.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/profiles/profile-state/profile-state.confirm-profile-suggestion.cs:604
- Description: After a first agent draft, a second draft prompts with "replaces what you typed" even though the person typed nothing.
- Suggestion: Neutral wording ("replaces unsaved changes on this form"), or skip the prompt when the only differences are unchanged agent-filled fields.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.prefill.cs:266
- Description: The not-page-bound `else` branch cannot run, because `FormPrefill.SelectAsync` only emits page-bound Prefill tools.
- Suggestion: Remove the `pageBound` local and the `else` branch. Note in the Design region that Prefill tools are page-bound by construction.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- M2 overlaps M1 only through the suggested regression test. They are kept separate: M1 is the defect, M2 is the coverage gap.
