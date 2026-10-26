# Round 2 — general
**Date:** 2026-10-26
**Scope reviewed:** fix delta `0179bd7e5..f9cd37489`. That is product commit `7ddf6c3e0`
("fix: refresh the assistant name draft from the server") plus two kitchen commits.
Product files: `source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor`
and `tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs`
(new).

## Summary

The fix covers both round-1 findings. On interactive init, `OnInitializedAsync` now calls
`LoadAsync(refreshSiteSettings: true)`. That always dispatches `FetchSiteSettings`, then copies
the fresh snapshot into the draft, so the sign-in `Version` is never what the form submits (M1).
A render-time `SyncUneditedAssistantDraft` compares the stored `Version` with the last synced
one. It copies an agent rename's name and version only when the draft still equals the last
synced values, so a typed edit is kept (M2). `tw-blazor` exempts "resetting a form draft from
state" from the actions-only rule. The Design region describes the new behavior. Risk is low.

Checks re-run in the claim worktree:

- `./bin/dev build`: 0 warnings, 0 errors.
- `web-spa-integration-tests --filter-class AiPage_Should_`: 2/2 passed. Both new tests assert
  on the form's `data-assistant-version` / `data-assistant-name` attributes, not on state. The
  stale-snapshot test also asserts two `GetSiteSettings` reads, which proves the page refetched.
- `web-spa-integration-tests --filter-tag Unit`: 383/383 passed, including the action catalog
  tests.

Not raised as findings (verified, no defect):

- If the person has typed a name and the agent renames at the same time, saving returns 409.
  The draft and its stale `Version` are kept. That is the intended optimistic-concurrency
  answer, and the task Goal limits the auto-sync to an unedited field. Reopening the page now
  refetches (the M1 fix), so the person can recover.
- The first render can start a second `LoadAsync` from `OnAfterRenderAsync` while the
  `FetchXaiApiKeyStatus` from init is still in flight. That duplicate Xai fetch was already
  there before this change. The init path syncs last, after its fresh `FetchSiteSettings`. So
  the draft settles on the server `Version`, and `IsBusy` disables the form while those actions
  run.
- `@{ SyncUneditedAssistantDraft(); }` is the only render-body statement block in the SPA.
  TimeWarp.State re-renders on a state change without calling `OnParametersSet`, so the render
  is the hook that sees an agent write. The member it calls lives in `@code`, as `tw-blazor`
  requires.

## Issues

None new. Prior IDs:

- M1 (bug): fixed. Verified by code reading and by
  `AiPage_Should_.Replace_A_Stale_Snapshot_With_The_Server_Version`.
- M2 (suggestion): fixed for an unedited field. Verified by
  `AiPage_Should_.Copy_An_Agent_Rename_Into_An_Unedited_Draft`.
