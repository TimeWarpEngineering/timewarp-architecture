# Round 2 — general
**Date:** 2026-11-05
**Scope reviewed:** the fix-pass delta `85dfe0f4e..29d388aff` (round-1 `base.sha` to HEAD), limited
to `proposal.md` and `task.md`. The changed review files are round-1 artifacts and are not
reviewed again.

## Summary

Fix pass 1 addressed M1–M4. I re-ran the new falsifiable claims, and they hold:

- `git grep -l "action.Command" -- …/web-spa/features` lists 6 files. 5 of them contain
  `DefaultApiHandler<`. The sixth is chat-state's `server-to-client-message-action.cs`.
- 19 files override `HandleError`.
- 16 `DefaultApiHandler<…>` declarations take a `Query`.
- `HandleError` is abstract on `ApiHandler` (`api-handler.cs:195`). The private
  `HandleApiResponseAsync` matches the `OneOf` straight to it (`:128-138`). That path runs after
  the semaphore is released, and the synthetic 499 for cancellation reaches it (`:77-86`).

The `task.md` Description, Checklist, and 461 path are corrected. Results now say 5 of 41. The
restatement still matches the original task, so there is no misread.

One new gap is in the `OnProblem` design that M1 added. The proposal says that without the
`OnProblem` leg the slot stays at `Running`, which implies that with the leg it does not. That is
true only when the failure comes back as a `SharedProblemDetails`.

## Issues

### Issue 1 — Severity: suggestion
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/proposal.md:385-415
- Description: Option D sets `Running` in `GetRequest` and `Failed` in `OnProblem`. `OnProblem` runs
  only from `HandleApiResponseAsync`. `ApiHandler.Handle` (`api-handler.cs:70-92`) has three paths
  that leave after `GetRequest` and never reach it, so the slot stays at `Running`:
  - A transport failure (server unreachable, DNS, CORS). `HttpApiService`
    (`source/foundation/foundation-contracts/services/http-api-service.cs`) catches only
    `OperationCanceledException` and JSON/`InvalidOperationException` on the problem body, so
    `HttpRequestException` propagates. `Handle` logs it and rethrows at `:87-92`.
  - A validator failure. `ValidateRequestAsync` throws `ValidationException` at `:123`, after
    `GetRequest` has run.
  - Under "A only", a hand-written `GetRequest` that sets `Running` and then returns `null`. That is
    the sanctioned skip at `:71`.

  A page that shows a spinner from `IsRunning` would spin for good after a network failure. That is
  the case lines 414-415 say D prevents. D's cost line ("one empty virtual on `ApiHandler`") is too
  low by whatever closes these paths.
- Suggestion: Name these paths in D, and say what D does about each one. Options:
  - Set `Running` in the base `Handle` (or a second virtual) only after `GetRequest` returns
    non-null.
  - Map unexpected exceptions in `Handle`'s catch to a synthetic problem, the way cancellation is
    mapped to 499, so `OnProblem` sees them. This is a platform behavior change.
  - Call `OnProblem` (or an `OnFault`) from a `catch`/`finally` before the rethrow.

  Then update D's scope line and the A-only check to match. Alternatively, accept the gap and say
  so: the slot can stay at `Running` after a thrown exception.
- Status: open
