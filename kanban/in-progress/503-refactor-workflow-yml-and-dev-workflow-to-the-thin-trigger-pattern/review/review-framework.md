# Review framework — task 503

## Budget (by-diff)

- Lines changed: 263
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

**Date:** 2026-11-24
**Host task:** kanban/in-progress/503-refactor-workflow-yml-and-dev-workflow-to-the-thin-trigger-pattern/
**Diff scope (round 2):** delta since round 1, commit `9ecb89ee7` (`fix(ci): classify push ci from the push diff with the pull request build rule`). Product files: `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/services/workflow-path-classes.cs`, `tests/tools/dev-cli-tests/workflow-path-classes-tests.cs`. Round 1 stays under `review/round-1/` and is not re-reviewed as a whole.
**Plan / brief:** Round 1 M1. A push must take `ci` from its own diff with `PullRequestBuildClassifier.RequiresBuild` and `.timewarp/dev.jsonc` `pullRequestBuild`. An unreadable diff turns every job on. `IsSkillPath` stays as it was.
**Effort:** 2 (general only; budget roster is general, no specialist axes)
**Reviewer roster:** general
**Session IDs:** implementer-claude session 096317c1-d66d-4d66-a125-3ad21261d284; review round 1 grok session 01a20b87-c057-7a43-b259-d149735018ca; review round 2 grok session 01a20b9a-f6b7-75d3-b1d4-5220661fa30c

Round 1 scope (frozen): branch `task/503-refactor-workflow-yml-and-dev-workflow-to-the-thin` vs merge-base `3cb120c9418ba3adf196760572627cf8870fe77f`. Product commit `000a4063f`. Budget then: 1292 lines, effort 3, turn cap 200.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
