# Review framework — task 501

**Date:** 2026-11-24
**Host task:** kanban/in-progress/501-disable-ci-playwright-in-workflow-yml/
**Diff scope:** branch `task/501-disable-ci-playwright-in-workflow-yml` vs `master` (merge-base). Product files: `.github/workflows/workflow.yml`, `tools/dev-cli/services/test-groups.cs`. Kanban move of this task is in the same range and is not product behavior.
**Plan / brief:** Remove the `playwright` leg and the screenshot upload from `ci-group` so pull requests no longer wait on `ci (playwright)`. Keep the suite and `TestGroups.Playwright` so `dev test --group playwright` still runs locally. Task 502 decides the suite's fate.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session `01a20b5f-d012-7c51-8126-f96ec33ed622` (2026-11-24). Implementer session `4241ffd3-b42d-4a89-92fe-5f377f8b1f11` is on `task.md`.

## Budget (by-diff)

- Lines changed: 135
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
