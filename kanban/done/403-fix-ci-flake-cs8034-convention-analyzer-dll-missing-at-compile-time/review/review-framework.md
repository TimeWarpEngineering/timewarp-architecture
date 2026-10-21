# Review framework

## Budget (by-diff)

- Lines changed: 136
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-22
**Host task:** kanban/to-do/403-fix-ci-flake-cs8034-convention-analyzer-dll-missing-at-compile-time/
**Diff scope:** branch `task/403-fix-ci-flake-cs8034-convention-analyzer-dll-missin` vs `master` (commit 3ac5aef2b). The only product file is `source/foundation/timewarp-contracts/timewarp-contracts.csproj`. The rest is the kanban folderize.
**Plan / brief:** The net10.0 convention-analyzer reference moves from `SetTargetFramework=TargetFramework=net11.0` to `SkipGetTargetFrameworkProperties` + `GlobalPropertiesToRemove=TargetFramework`, so the solution build has one analyzer project instance.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, ganda task work headless), session id not exposed

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Address the diff and surrounding call sites. Re-verify falsifiable claims against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.
