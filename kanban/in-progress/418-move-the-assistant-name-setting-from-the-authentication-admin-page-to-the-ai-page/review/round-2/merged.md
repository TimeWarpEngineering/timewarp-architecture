# Round 2 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:89
- Description: The AI page kept the sign-in `Version`, so a save after any other write
  returned 409.
- Source: general (round 1)
- Disposition notes: Fixed in `7ddf6c3e0`. Interactive init calls
  `LoadAsync(refreshSiteSettings: true)`, which always dispatches `FetchSiteSettings` and then
  `SyncAssistantDraft`. `AiPage_Should_.Replace_A_Stale_Snapshot_With_The_Server_Version` seeds
  version 1, serves version 7, and asserts the form carries 7 after two settings reads.
  It passed in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor:132
- Description: An agent rename while the page is open left the form's name and `Version` stale.
- Source: general (round 1)
- Disposition notes: Fixed in `7ddf6c3e0`. `SyncUneditedAssistantDraft` runs at render. When the
  stored `Version` advances and the draft still matches the last sync, it copies the new name
  and `Version`. A typed edit is kept, and a save after a concurrent rename returns 409, which is
  the intended concurrency answer. `AiPage_Should_.Copy_An_Agent_Rename_Into_An_Unedited_Draft`
  passed in round 2.

## Duplicates / conflicts

- None. Round 2 raised no new findings.
