# Review framework — task 458-004

**Date:** 2026-11-26
**Host task:** kanban/in-progress/458-004-one-problem-details-shape-on-the-wire-and-in-open-api-including-validation-400/
**Diff scope:** branch `task/458-004-one-problem-details-shape-on-the-wire-and-in-open` vs `master` (merge-base `f5f2b2add`, 3 commits, 1128 lines). Product change is `112b8d097`. Kitchen move and land commits are `fbd854965` and `10a4776c8`.
**Plan / brief:** One problem-details shape on the wire (`application/problem+json`, including validation 400) and one `TimeWarpFoundationTypesSharedProblemDetails` component in OpenAPI (`errors` as a map of string arrays, extension members allowed). Validator operations document 400 with that schema. Default, 400, bearer 401/403, payment 402/503, and Entra 400/403 reference it. `MicrosoftAspNetCoreMvcProblemDetails` is gone. `GET /api/hello` without `name` is the wire check.
**Effort:** 3 (by-diff, 1128 lines)
**Reviewer roster:** general
**Session IDs:** grok `01a214cb-7cf7-73d0-98bb-94112a3e46b0` (review oracle). Implementer session `f65f0311-2aa8-474a-86cf-18838ef9b251` (claude).

## Budget (by-diff)

- Lines changed: 1128
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
