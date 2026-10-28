# Round 3 — merged findings
**Date:** 2026-10-28
**Sources:** general
**Scope:** the fix-pass delta `31bdd4ae7..0057d58aa` (product commit `727527b21`), excluding `kanban/`.

## Counts

Totals for all three rounds: M1–M19 are from round 1 and M20–M25 from round 2. Round 3 raised no
new findings.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 11 | 0 |
| nit | 0 | 9 | 2 |

## Prior findings

M1–M19 were fixed in round 2 (`review/round-2/merged.md`). The delta does not touch them.

| ID | Severity | Status | Evidence |
|----|----------|--------|----------|
| M20 | suggestion | fixed | Two tests in `ai-page-assistant-draft-tests.cs`: an edited draft keeps its old version and its edit when its own server field changes. The other draft still adopts the new version. A regression that always adopts the version fails both tests. |
| M21 | nit | wontfix | Unchanged. See `review/round-2/merged.md`. |
| M22 | nit | wontfix | Unchanged. See `review/round-2/merged.md`. |
| M23 | nit | fixed | `DesignSystemPage.razor:30` `Title="@Title"`. |
| M24 | suggestion | fixed | `PageTitleBar.IsPageHeading` (bool, default true). The sample passes false. A contract test asserts that the sample has no h1. |
| M25 | nit | fixed | A blank title omits only the heading. A render test covers it with both slots and with actions only. |

## New issues (round 3)

None.

## Duplicates / conflicts

- None.
