# Drop dead ISender plumbing from SPA base API handlers and derived constructors

## Description

`DefaultApiHandler.Sender` (`source/container-apps/web/projects/web-spa/features/base/default-api-handler.cs:20`)
and `FileResponseApiHandler.Sender` (`features/base/file-response-api-handler.cs:19`) are
written-but-never-read private fields — `HandleError` already dispatches error toasts via the
generated `ToastNotificationState.AddProblemDetails(...)`. After task 196 removed the last
derived uses of an injected sender, ten handler constructors still thread `ISender sender`
through DI purely to feed these dead fields.

Keeping a live `ISender` on the base of every SPA handler preserves exactly the affordance
TWA0022 (task 196) exists to remove — deleting it is defence in depth on top of the analyzer.

Origin: task 196 round-1 review finding M4 (`kanban/.../196-.../review/round-1/merged.md`),
deferred as out of that diff's blast radius.

## Checklist

- [ ] Remove the `Sender` field and `ISender sender` ctor parameter from `DefaultApiHandler`
- [ ] Remove the same from `FileResponseApiHandler`
- [ ] Update the ~10 derived handler constructors (and their DI call sites, if any name the parameter)
- [ ] Reconcile Design regions on both base handlers
- [ ] `dev build` 0/0 and web-spa test suites green

## Notes

- TWA0022 does not flag the dead fields (no `Send` invocation) — this is pure debt removal.

### Plan

1. Drop `Sender` field + `ISender sender` ctor param from `DefaultApiHandler` and `FileResponseApiHandler`; update Design regions to note errors go only via `ToastNotificationState` (no mediator on the base).
2. Update all 10 derived `DefaultApiHandler` ctors to stop threading `ISender` (no `FileResponseApiHandler` subclasses; no explicit DI call sites; no test constructors).
3. `dev build` 0/0; run web-spa related test suites.
4. Folderize for Phase 4b review → disposition → Results → done → PR.

Files (12): both bases + weather-forecasts fetch, authorization fetch-current-user, profile fetch, role fetch/create/set-permissions, principal fetch/set-roles, credentials fetch/revoke.

## Session

- Implementer: grok headless profile=implementer-grok worktree=task-197-… (2026-08-23 UTC)
- Phase 1: moved to in-progress; exploration confirmed Sender unread on both bases, 10 derived, 0 FileResponse subclasses.
