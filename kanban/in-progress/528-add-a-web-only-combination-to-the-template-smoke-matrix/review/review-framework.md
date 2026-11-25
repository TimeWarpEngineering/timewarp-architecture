# Review framework — task 528

**Date:** 2026-11-26
**Host task:** kanban/in-progress/528-add-a-web-only-combination-to-the-template-smoke-matrix/
**Diff scope:** branch `task/528-add-a-web-only-combination-to-the-template-smoke` vs `master` (`master...HEAD`). Product: `tools/dev-cli/endpoints/template-smoke-command.cs`, `tools/dev-cli/services/template-smoke/template-smoke-matrix.cs`, `template-smoke-runner.cs`, `template-smoke-test-runs.cs`, `template-smoke-test-tables.cs`, `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`. Kanban: task file moved to-do → in-progress with Results.
**Plan / brief:** Add `SmokeWebOnly` (`--api false --grpc false --yarp false`, web and postgres on) to `dev template-smoke --matrix`, with `ExcludedFamilies` api, grpc, and yarp so an orphaned test project from any of those families fails the smoke. Nightly `template-smoke --matrix --app-tests` picks it up. Publish coverage for `aca` and `aca-neon` is the generated app's aspire-tests under `--app-tests`.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review oracle grok session `01a214f8-7953-7ee1-929d-42ae433fbde6`; implementer session `9ed4eca8-118b-4161-a5bf-46b6515adb34` (recorded on task.md)

## Budget (by-diff)

- Lines changed: 281
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
