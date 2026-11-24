# Review framework — task 516

**Date:** 2026-11-24
**Host task:** kanban/in-progress/516-add-project-and-class-method-filter-options-to-dev-test/
**Diff scope:** branch `task/516-add-project-and-class-method-filter-options-to-dev` vs merge-base `b9110665f` (`master`). Three commits (move, feature, land). Product diff: `AGENTS.md`, `tools/dev-cli/endpoints/test-command.cs`, `tools/dev-cli/services/dotnet-test-arguments.cs`, `tools/dev-cli/services/test-project-selection.cs`, `tests/tools/dev-cli-tests/dev-cli-tests.csproj`, `tests/tools/dev-cli-tests/dotnet-test-arguments-tests.cs`, `tests/tools/dev-cli-tests/test-project-selection-tests.cs` (441 insertions, 57 deletions including the kanban move).
**Plan / brief:** Task 516 requirements and `## Goal (implementer restatement)` in `task.md`. Targeted `dev test --project` / `--filter-class` / `--filter-method` so agents stop using raw `dotnet test <csproj>` from the repo root.
**Effort:** 2 (general only; host budget by-diff)
**Reviewer roster:** general
**Session IDs:** implementer claude `82453250-7c5e-418e-b992-ca41b6d172e4`; review grok `01a20d0e-44c9-7df2-b28b-c8838c79be5e`

## Budget (by-diff)

- Lines changed: 498
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
