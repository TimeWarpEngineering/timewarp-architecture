# Review framework — task 458-007

**Date:** 2026-11-26
**Host task:** kanban/in-progress/458-007-document-or-exclude-browser-navigation-routes-and-liveness-in-open-api/
**Diff scope:** Round 2 reviews commit `fd3552530` against its parent (the fix for round 1). Product files: `web-server-open-api-excluded-routes.cs`, `llms.txt`, `open-api-discovery-coverage.cs`, `open-api-discovery-coverage-tests.cs`, and new `open-api-served-route-coverage-tests.cs`. Round 1 stays under `review/round-1/`.
**Plan / brief:** task.md requirements and `## Goal (implementer restatement)`. Round 2 re-checks M1 (`GET /alive`), M2 (unlisted families in `info.description`), and M3 (dictation CONNECT and a method-aware walk).
**Effort:** round 1 was 3; round 2 is 2 (general only; by-diff budget)
**Reviewer roster:** general
**Session IDs:** round 1 grok session 01a21512-9948-7c90-8a6c-a0777e930e7a (GANDA_SESSION 2759271); round 2 grok session 01a21525-b904-71a3-be9e-aacba7815fa7

## Budget (by-diff)

- Lines changed: 568
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
