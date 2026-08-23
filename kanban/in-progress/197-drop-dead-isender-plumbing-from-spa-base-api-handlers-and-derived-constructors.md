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

## Session

- Implementer launch: host=headless profile=implementer-grok provider=profile-default worktree=/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/task-197-drop-dead-isender-plumbing-from-spa-base-api-handl (2026-08-23 UTC)
- Implementer launch: host=headless profile=implementer-grok provider=profile-default worktree=/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/task-197-drop-dead-isender-plumbing-from-spa-base-api-handl (2026-08-23 UTC)
