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

- [x] Remove the `Sender` field and `ISender sender` ctor parameter from `DefaultApiHandler`
- [x] Remove the same from `FileResponseApiHandler`
- [x] Update the ~10 derived handler constructors (and their DI call sites, if any name the parameter)
- [x] Reconcile Design regions on both base handlers
- [x] `dev build` 0/0 and web-spa test suites green

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
- Phase 4: ISender removed from both bases + 10 derived handlers; Design regions reconciled; `dotnet run tools/dev-cli/dev.cs -- build` 0/0; `web-spa-integration-tests` 15 passed / 1 skipped.
- Phase 4b: folderized; effort-1 general review → 0 open; disposition `clean` under `review/`.

## Results

### What was implemented

Removed dead `ISender` / `Sender` plumbing from SPA `DefaultApiHandler` and `FileResponseApiHandler`, and stopped threading `ISender` through all ten derived `DefaultApiHandler` constructors. Error toasts continue via `ToastNotificationState.AddProblemDetails` in `HandleError`. Design regions on both bases document that the base does not hold or use `ISender` (defence in depth after TWA0022 / task 196).

### Files changed

- `source/container-apps/web/projects/web-spa/features/base/default-api-handler.cs`
- `source/container-apps/web/projects/web-spa/features/base/file-response-api-handler.cs`
- `…/weather-forecasts-state.fetch-weather-forecasts.cs`
- `…/authorization-state.fetch-current-user.cs`
- `…/profile-state.fetch-profile-data.cs`
- `…/role-state.fetch-roles.cs`, `role-state.create-role.cs`, `role-state.set-role-permissions.cs`
- `…/principal-state.fetch-principals.cs`, `principal-state.set-principal-roles.cs`
- `…/credentials-state.fetch-credentials.cs`, `credentials-state.revoke-credential.cs`
- Kanban task folder + `review/` (framework, round-1 general/merged, disposition)

### Key decisions / deviations

- No `FileResponseApiHandler` subclasses existed; no explicit DI call sites named the parameter; no test constructors required updates.
- Phase 4b effort 1 (general only) per default.

### Test outcomes

- `dotnet run tools/dev-cli/dev.cs -- build` — 0 warnings / 0 errors
- `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release` — 15 passed, 1 skipped (quarantined weather SPA fetch, task 058)

### Phase 4b review

- Rounds: 1
- Roster / effort: general only (effort 1)
- Final counts: 0 open / 0 fixed / 0 wontfix across bug|suggestion|nit
- Disposition: **clean**
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke**

```bash
# from repo root (claimed task worktree)
rg -n 'ISender sender|private readonly ISender Sender' source/container-apps/web/projects/web-spa/features/base/
rg -n 'ISender sender' source/container-apps/web/projects/web-spa/features -g '*state*.cs'
```

**Expect**

- First command: no matches (bases have no Sender field / ISender ctor param).
- Second command: no matches in product handler ctors (Design/comment mentions of ISender elsewhere may remain, e.g. chat-hub / toast Design).

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build completed successfully! 0 Warning(s) 0 Error(s)

cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release
# expect: Passed! failed: 0 (1 skipped quarantine ok)
```

**Not in scope:** browser UX change (constructor DI only); live Aspire weather SPA fetch (still quarantined task 058).
