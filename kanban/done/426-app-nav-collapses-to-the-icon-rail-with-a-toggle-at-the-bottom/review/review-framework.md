# Review framework

## Budget (by-diff)

- Lines changed: 729
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-28
**Host task:** kanban/in-progress/426-app-nav-collapses-to-the-icon-rail-with-a-toggle-at-the-bottom/
**Diff scope:** branch `task/426-app-nav-collapses-to-the-icon-rail-with-a-toggle-a` vs `master` (merge base), commit 591210570, product paths only
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: a bottom nav toggle that switches the shell column between the full 240px width and the existing 68px rail. It is a TimeWarp.State action, the choice is persisted per browser, there is no first-paint flash, and the rail rules are reused.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general (the review oracle did the review itself, with no subagent)
**Session IDs:** review oracle (claude, claude-opus-5-5), ganda task work review node 2026-10-28

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Review the diff and the call sites around it. Re-check claims that can be tested against the repo.
- Earlier rounds are never edited. New work goes in `round-(N+1)/`.
