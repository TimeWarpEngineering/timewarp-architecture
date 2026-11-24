# Review framework

## Budget (by-diff)

- Lines changed: 268
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 517

**Date:** 2026-11-24
**Host task:** kanban/in-progress/517-make-dev-build-quiet-by-default-with-verbose-to-stream-full-ms-build-output/
**Diff scope:** branch `task/517-make-dev-build-quiet-by-default-with-verbose-to` vs `master` (ahead 3, behind 2). Product commit `1acd950aa`. The two commits on master that this branch lacks are kanban lands only (`150790d65`, `9fff370f8`); they do not touch `tools/dev-cli` or `tests/tools/dev-cli-tests`.
**Plan / brief:** task.md `## Goal (implementer restatement)` and the original requirements. Default `dev build` captures MSBuild output, prints one summary line on success, prints the captured output and exits non-zero on failure, adds `--verbose` / `-v` to stream, and keeps `--quiet` / `-q` as the default.
**Effort:** 2 (general only; the budget names a single roster axis)
**Reviewer roster:** general
**Session IDs:** review oracle and general reviewer, Grok 4.7, session `01a20e10-17ad-7ba1-879e-9ed73ba2c14c` (2026-11-24). No reviewer subagent: this oracle is print mode and inspects in the foreground.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
