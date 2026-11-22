# Review framework — task 498

## Budget (by-diff)

- Lines changed: 1462
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

**Date:** 2026-11-22
**Host task:** kanban/in-progress/498-bring-all-nu-get-pins-current-mediator-15-0-0-beta-10-state-12-0-0-beta-19-contracts-2-0-0-beta-34-and-the-rest-of-the-outdated-set/
**Diff scope:** branch `task/498-bring-all-nu-get-pins-current-mediator-15-0-0-beta` vs `master` (9 commits, 40 files, +856/−606). Unstaged kitchen edits are this review, not the product diff.
**Plan / brief:** Bring every `Directory.Packages.props` pin `ganda nuget outdated` reports forward, except the seven EF Core / Npgsql pins (task 494). Aspire 17 is in scope: CLI bundle, `Microsoft.NET.Sdk` AppHost, `AddDotnetProject`, and CI installs the pinned Aspire CLI wherever the AppHost builds.
**Effort:** 3 (general only; budget roster is general)
**Reviewer roster:** general
**Session IDs:** reviewer-grok session 01a20272-ddfd-7171-87fc-672623d3891e (2026-11-22). An earlier review oracle session 01a20272-a2e4-74a2-95ab-7f6b1c9327ac exited in one turn with no round artifacts.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
