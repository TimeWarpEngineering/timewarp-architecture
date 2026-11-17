# Round 2 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 0 |

## Resolved prior (round 1, re-verified against `85dfe0f4e..29d388aff`)

- **M1 — suggestion — fixed.** Option D adds `OnProblem` before `HandleError`. The A-only check
  covers all three assignments, and the scope line names the base change. M5 is a gap in this new
  design.
- **M2 — suggestion — fixed.** B states the required-on-every-fetch rule and its cost (up to 16).
  The opt-in alternative is stated, with 4f then resting on a test. 16 `Query` handlers was
  re-verified.
- **M3 — nit — fixed.** The count is 5 of 41, and the grep is cited. Re-run: 6 files, 5 of them
  `DefaultApiHandler`s. `task.md` Results matches.
- **M4 — nit — fixed.** The Description and Checklist point at `proposal.md`. The split item is
  replaced, and the 461 path is `kanban/in-progress/`.

## Issues

### M5 — Severity: suggestion — Status: fixed
- File: kanban/in-progress/464-research-generate-front-end-ux-pieces-from-contracts-counterpart-to-461/proposal.md:385-415
- Description: In Option D, the slot is set to `Running` in `GetRequest` and to `Failed` only in
  `OnProblem`. `OnProblem` runs only when the failure comes back as a `SharedProblemDetails`. Three
  paths in `ApiHandler.Handle` leave after `GetRequest` and skip `OnProblem`, so the slot stays at
  `Running` for good:
  - a transport `HttpRequestException`, rethrown at `api-handler.cs:87-92`
  - a `ValidationException` thrown at `:123`
  - under A-only, a hand-written `GetRequest` that sets `Running` and then returns `null`

  Lines 414-415 imply the `OnProblem` leg closes the stuck-`Running` case. It does not close it for
  a network failure, and D's scope line understates the base-class change.
- Suggestion: Name the thrown-exception and null-skip paths in D, and pick a remedy:
  - set `Running` in the base after a non-null `GetRequest`
  - map unexpected exceptions to a synthetic problem, as 499 is mapped
  - call a fault hook before the rethrow

  Otherwise state that the gap is accepted. Update the scope line and the A-only check to match.
- Source: general
- Disposition notes: fixed in fix pass 2. Option D now has a table of every exit from `ApiHandler.Handle` and the hook that closes it. Remedy: `Running` moves to a new `OnStarted(TRequest)` virtual called after the null skip, and the existing catch calls `OnProblem` with a synthetic problem before `throw;` (validation still throws). The rejected remedies (map to a problem through `HandleError`, set in the base without a hook) are named with reasons. Two accepted gaps are named: `DefaultApiHandler.HandleFileResponse` throwing, and a hand-written `HandleSuccess` throwing before its write. The scope line and the A-only check match. The check also flags `Running` in `GetRequest`.

## Duplicates / conflicts

- None (single reviewer).
