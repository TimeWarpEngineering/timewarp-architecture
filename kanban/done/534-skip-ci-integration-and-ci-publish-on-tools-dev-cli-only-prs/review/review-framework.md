# Review framework — task 534

**Date:** 2026-11-27
**Host task:** kanban/in-progress/534-skip-ci-integration-and-ci-publish-on-tools-dev-cli-only-prs/
**Diff scope:** branch `task/534-skip-ci-integration-and-ci-publish-on-tools-dev` vs merge-base `af245adde` (master). Product diff: `.github/workflows/workflow.yml`, `tools/dev-cli/services/workflow-path-classes.cs`, `tools/dev-cli/endpoints/workflow-template-repository.cs`, `tools/dev-cli/services/skill-spec-lint.cs`, `tests/tools/dev-cli-tests/workflow-path-classes-tests.cs`. 513 lines changed.
**Plan / brief:** Skip `ci (integration)`, `ci (publish)`, and unrelated `skill-lint` on a pull request that changes only `tools/dev-cli/` (plus non-build paths). Keep `ci (fast)` and `template-smoke`. A partial ci-group run must not write the `ci` green-key marker. Push, dispatch `mode=full`, and an unknown diff keep every leg. Generated-app workflow stays compatible.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** session not reported

## Budget (by-diff)

- Lines changed: 513
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/` and the task kitchen (`task.md` Results, Session, Learnings)
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
