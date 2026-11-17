# Round 1 — general
**Date:** 2026-11-05
**Scope reviewed:** faf68cf1b, the product change in all 20 files under `source/container-apps/web/features/admin/roles/` and `source/container-apps/web/features/feedback/`, plus the call sites around them.

## Summary

The change adds `RoleContractMap.Project(Role)` and `FeedbackContractMap.Project(FeedbackAttachment)`.
Each reads the five shared members once into a `readonly record struct`. GetRole and GetRoles wrap
`RoleProjection` into `GetRole.Response` / `GetRoles.RoleDto`. `AttachmentsAsync` (used by GetFeedback
and `AdminViewAsync`) and the upload handler wrap `AttachmentProjection`. Every response, DTO,
`Item`, `FeedbackAdminView`, `FeedbackActivityEntry`, and attachment constructor call in the Roles
and Feedback application layers now uses named arguments. Risk is low: the compiler checks every
named argument, the member values and their order are the same as before, and no test file changed.

## Verification

- A search for constructor calls in `*-application.cs` under both slices found positional
  arguments only in `FeedbackListFilter`, `EmailMessage`, `FeedbackPage` (in-memory store),
  `EventId`, `MemoryStream`, and exception constructors. None of these is a response projection.
  The task scope (and the Results "out of scope" note) leaves them alone.
- `RoleContractMap.Project` reads `role.AgentScopes` as is, with no `AgentScopeOrder.OrderKnown`
  call (task 478 is on the base). That meets the conditional requirement.
- The upload handler no longer imports `DownloadFeedbackAttachmentContract`. The download path now
  comes only from `FeedbackContractMap.Project`, so it has one source.
- `GetRole.Response` and `GetRoles.RoleDto` remain separate types with the same wire shape. The
  shared member list is in one place, and each handler only wraps it. This is the pattern the
  requirement allows.
- Design regions in the touched handlers and both maps describe the shared projection correctly.
  The upload handler's "GetFeedback and FeedbackAdminView share" matches the `AttachmentsAsync`
  callers. The new file has Purpose and Design regions (TWA0004).
- Gates re-run in this worktree: `./bin/dev build` gave 0 warnings and 0 errors.
  `./bin/dev test --no-build -q` gave `23 of 23 projects passed`.

## Issues

None.
