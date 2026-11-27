# Review framework — task 535

**Date:** 2026-11-27
**Host task:** kanban/in-progress/535-compile-tools-dev-cli-dev-cs-non-aot-during-dev-build/
**Diff scope:** branch `task/535-compile-tools-dev-cli-dev-cs-non-aot-during-dev` vs `master` (three-dot). 5 files, +138 / −14. Product: `tools/dev-cli/endpoints/build-command.cs`, `tools/dev-cli/services/build-output.cs`, `tools/dev-cli/services/template-smoke/template-smoke-dev-cli.cs`, `tests/tools/dev-cli-tests/build-output-tests.cs`. Kitchen: `task.md`.
**Plan / brief:** `## Goal (implementer restatement)` in `task.md` — `./bin/dev build` compiles `tools/dev-cli/dev.cs` with a plain Release `dotnet build` (no NativeAOT link) after the solution and unlisted test projects, and `--clean` cleans that runfile too.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implementer session `9099f34a-e60e-4a43-bef3-4a3cea3b4fc8` (on `task.md`). This review launch did not pass a vendor session id.

## Budget (by-diff)

- Lines changed: 152
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
