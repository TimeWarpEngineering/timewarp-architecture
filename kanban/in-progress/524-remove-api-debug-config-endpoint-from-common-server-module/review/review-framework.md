# Review framework — task 524

**Date:** 2026-11-25
**Host task:** kanban/in-progress/524-remove-api-debug-config-endpoint-from-common-server-module/
**Diff scope:** branch `task/524-remove-api-debug-config-endpoint-from-common` vs merge-base `f5c492088` (`master`). Product diff is `651ea4cd7`: `common-server-module.cs`, `global-usings.cs`, `open-api-discovery-coverage.cs`, and new `common-server-module-endpoints-tests.cs`. Later commits are kanban only.
**Plan / brief:** Remove `GET /api/debug-config` from `CommonServerModule`, keep `ConfigureEndpoints` as an empty `IAspNetModule` hook, update the Design regions and XML docs, and add a Docker-free Development 404 regression test.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a20ffa-57ea-7711-a899-b092956ce284` (2026-11-25). Implementer session `970ee484-a715-4332-9d37-9e0ed7254053` is on `task.md`.

## Budget (by-diff)

- Lines changed: 140
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
