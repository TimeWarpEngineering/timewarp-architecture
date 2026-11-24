# Review framework — task 503

## Budget (by-diff)

- Lines changed: 1292
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

**Date:** 2026-11-24
**Host task:** kanban/in-progress/503-refactor-workflow-yml-and-dev-workflow-to-the-thin-trigger-pattern/
**Diff scope:** branch `task/503-refactor-workflow-yml-and-dev-workflow-to-the-thin` vs merge-base `3cb120c9418ba3adf196760572627cf8870fe77f` (master). Product commit `000a4063f`. 18 files, +784/−508. Untracked kitchen file: this framework (host budget stub, completed in this round).
**Plan / brief:** Thin-trigger `workflow.yml`: one local composite setup action, `dev workflow --phase classify` emits `ci` / `skill` / `template`, ingress golden moves into `dev workflow --phase test --group fast`, `concurrency:` cancels superseded pull requests and pushes, four Aspire publish uploads removed.
**Effort:** 3 (general only; budget roster is general, no specialist axes)
**Reviewer roster:** general
**Session IDs:** implementer-claude session 096317c1-d66d-4d66-a125-3ad21261d284; review grok session 01a20b87-c057-7a43-b259-d149735018ca

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
