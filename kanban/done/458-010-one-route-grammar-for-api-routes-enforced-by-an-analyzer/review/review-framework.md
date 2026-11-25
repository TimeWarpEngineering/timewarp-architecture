# Review framework — task 458-010

**Date:** 2026-11-26
**Host task:** kanban/in-progress/458-010-one-route-grammar-for-api-routes-enforced-by-an-analyzer/
**Diff scope:** branch `task/458-010-one-route-grammar-for-api-routes-enforced-by-an` vs merge-base `4a059739621394d2af0babeb82217cd63f8a3bc8` (master). Four commits, 64 files, 569 insertions, 162 deletions (731 lines changed). Product surface is TWA0051, `route-grammar.md`, the `[ApiRoute]` renames, the ingress golden, and the tests and skill examples that name those paths.
**Plan / brief:** One kebab route grammar under `api/`, enforced by a new convention analyzer on every `[ApiRoute]` string, with the non-conforming demo routes renamed and the ingress golden regenerated. Browser sign-out stays with task 458-007.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a213ac-f39c-7d62-876f-ebedea64e08e` (ganda session 1735810). Implementer session is on `task.md`.

## Budget (by-diff)

- Lines changed: 731
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
