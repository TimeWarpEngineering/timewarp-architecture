# Review framework — task 504

**Date:** 2026-11-24
**Host task:** kanban/in-progress/504-move-ci-onto-wslc-self-hosted-runners-and-adopt-green-key-skip/
**Diff scope:** branch `task/504-move-ci-onto-wslc-self-hosted-runners-and-adopt` vs merge-base `789928877` (`master...HEAD`). Product commits: `633cc973c`. Kitchen move/land commits are in the same range. Ahead 3, behind 4.
**Plan / brief:** task.md Description and Requirements. Same-repo CI on `["self-hosted", "twe-001", "wslc"]` unless `CI_HOSTED_ONLY` is true; hosted-only NuGet cache; Docker leg stays on `ubuntu-latest`; green-key skip matching timewarp-ganda 469/470/711.
**Effort:** 3 (by-diff, 1039 lines changed)
**Reviewer roster:** general
**Session IDs:** review oracle grok session 01a20bc0-d828-7a00-9ada-cb96335b6798 (2026-11-24). Implementer session is on task.md.

## Budget (by-diff)

- Lines changed: 1039
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Goal check

The implementer restatement matches the task. It adds `schedule` to the hosted list; the requirements bullet does not name schedule, and the nightly template-smoke job is a schedule event that stays hosted because its app tests need Docker. That is the Docker-leg rule, not a misread. Per-gate markers are how this workflow's independent gates skip without treating a partial pass as a full pass. A full same-repo pass still emits a marker for each gate that passed.
