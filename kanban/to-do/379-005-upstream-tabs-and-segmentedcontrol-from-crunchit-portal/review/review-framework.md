# Review framework

## Budget (by-diff)

- Lines changed: 920
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Framework — task 379-005

**Date:** 2026-10-22
**Host task:** kanban/to-do/379-005-upstream-tabs-and-segmentedcontrol-from-crunchit-portal/
**Diff scope:** branch `task/379-005-upstream-tabs-and-segmentedcontrol-from-crunchit-p` vs `master` (merge base), head `836d14c45`. Product paths: `source/container-apps/web/projects/web-spa/components/composites/{Tabs,SegmentedControl}.razor(.css)`, `roving-selection.cs`, `components/overview.md`, `features/style-guide/pages/StyleGuidePage.razor`, and three test files under `tests/container-apps/web/web-spa-integration-tests/components/`.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Checklist`: generic Tabs and SegmentedControl, value + `ValueChanged` only, roving tabindex, keyboard via a pure helper, tokens only, style guide adoption, render tests.
**Effort:** 3 (by-diff budget); roster axes general only
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), ganda task work headless; session id not exposed to the oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
