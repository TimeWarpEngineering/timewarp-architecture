# Review framework

## Budget (by-diff)

- Lines changed: 1099
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-25
**Host task:** kanban/in-progress/413-remove-client-names-from-template-source-tests-shipped-skills-and-tools-add-a-check-that-keeps-them-out/
**Diff scope:** branch `task/413-remove-client-names-from-template-source-tests-shi` vs merge-base `a90e284c4` (commit `4a0af0ed9`), kanban/ excluded
**Plan / brief:** task.md Required outcome + `## Goal (implementer restatement)`
**Effort:** round 1: 3; round 2: 2 (budget by-diff); roster axes: general
**Round 2 scope:** fix-pass delta `f384c75b7..4ccd816aa` (product commit `70a5819ed`), kanban/ excluded
**Round 3 scope:** plain-search rework `f04972a83..973480be5` (product commit `4fa6334cb`), kanban/ excluded; effort 3
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle (ganda task work headless; session id not exposed)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
