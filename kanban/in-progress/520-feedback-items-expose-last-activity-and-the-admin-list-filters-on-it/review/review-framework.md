# Review framework — task 520

**Date:** 2026-11-25
**Host task:** kanban/in-progress/520-feedback-items-expose-last-activity-and-the-admin-list-filters-on-it/
**Diff scope:** branch `task/520-feedback-items-expose-last-activity-and-the-admin` vs `master` (23 files, +919/−121). Product change is `LastActivityAt` on the feedback aggregate, the Postgres migration and backfill, the admin list filter and sort, and the tests named in `## Results`. Round 2 re-checks that diff after the PR #692 compile fix in `feedback-owner-name-tests.cs`. Audit baselines shrink for EF-generated files after ganda 723.
**Plan / brief:** task 520 — a `feedback:read` poller can ask which feedback items changed since a time, without reading every item's detail.
**Effort:** 3 (by-diff, 1040 lines changed)
**Reviewer roster:** general
**Session IDs:** round 1 reviewer-grok session 01a211e9-0052-7612-b27f-f40bb2e390fc; round 2 reviewer-grok session 01a212bb-86ba-7273-8559-294134f9f34c

## Budget (by-diff)

- Lines changed: 1040
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
