# Review framework — task 458-008

**Date:** 2026-11-26
**Host task:** kanban/in-progress/458-008-stop-publishing-ignored-user-id-and-o-data-query-parameters-on-admin-queries/
**Diff scope:** branch `task/458-008-stop-publishing-ignored-user-id-and-o-data-query` vs merge-base `2e90c246` (`master`). Product commit `e25884ca4`. 106 files, 1682 lines changed. Untracked kitchen only: this `review/` tree.
**Plan / brief:** Stop publishing unread `userId` and OData query members (`top`, `skip`, `filter`, `orderBy`, `returnTotalCount`) that `[AuthApiRequest]` and `[OpenDataQueryParameters]` stamped onto admin and identity contracts. Record whether `IAuthApiRequest.UserId` belongs on the wire. Add a handler-read check only if it can be done without inference.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** review oracle grok session `01a213f4-0f56-78d2-90e2-6f5e9bcb0d5c` (2026-11-26). Implementer session `a55ae55e-9096-452b-a518-4425f1fe25cd` (2026-11-25).

## Budget (by-diff)

- Lines changed: 1682
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
