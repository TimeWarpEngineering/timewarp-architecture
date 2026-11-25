# Review framework — task 523

**Date:** 2026-11-26
**Host task:** kanban/in-progress/523-dev-test-runs-the-classes-in-a-test-file-so-walk-gates-run-playwright-classes/
**Diff scope:** branch `task/523-dev-test-runs-the-classes-in-a-test-file-so-walk` vs merge-base `d27ec05f8` (master). Product diff: `dev test` file arguments, mapping tests, playwright comments, AGENTS.md Tests row. Kanban move is in the same range.
**Plan / brief:** task.md Requirements and Goal (implementer restatement). `dev test` takes test file paths: runfile guard runs `dotnet run --file`; a file under `tests/` runs its project filtered to the test classes that file declares; helper and stray files fail naming the file; `--group`, `--project`, and `--filter-class` are refused with files.
**Effort:** 2 (by-diff, 767 lines). Roster axes: general only.
**Reviewer roster:** general
**Session IDs:** review oracle Grok session 01a2141b-1557-72b3-8bda-73ac5306b0b0 (2026-11-26). Implementer session da2cde51-352e-4f81-9fba-e3223c82937e (2026-11-25).

## Budget (by-diff)

- Lines changed: 767
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
