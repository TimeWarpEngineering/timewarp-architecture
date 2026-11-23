# Review framework — task 493

**Date:** 2026-11-17
**Host task:** kanban/in-progress/493-adopt-the-shared-pull-request-build-classifier-and-the-one-workflow-ci-standard/
**Diff scope:** branch `task/493-adopt-the-shared-pull-request-build-classifier-and` vs merge-base `71e78941` (master). Product files: `.github/workflows/workflow.yml`, `.timewarp/dev.jsonc`, `Directory.Packages.props`, `tools/dev-cli/endpoints/workflow-command.cs`. Kanban move of this task is in the same range and is not product behavior.
**Plan / brief:** Adopt the shared `PullRequestBuildClassifier` (TimeWarp.Nuru.DevCli 4.0.0-beta.15) so every pull request reports `ci`, and a pull request that changes only non-build paths skips build and test. Remove the `pull_request` paths filter. Keep the push filter. Declare this repo's extra roots in `.timewarp/dev.jsonc`.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review oracle grok 4.7, session 01a1e5d7-16f8-74f3-961a-a025bd1f3c40 (max-turns 120). Implementer session is on task.md.

## Budget (by-diff)

- Lines changed: 316
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

The roster is a single general axis, so this session wrote `review/round-1/general.md` directly. No second reviewer was spawned.
