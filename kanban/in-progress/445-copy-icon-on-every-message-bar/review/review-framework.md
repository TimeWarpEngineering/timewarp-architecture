# Review framework — task 445

**Date:** 2026-10-30
**Host task:** kanban/in-progress/445-copy-icon-on-every-message-bar/
**Diff scope:** commit 9bdfe4cad (`task/445-copy-icon-on-every-message-bar` vs `master`): product (`source/`), tests (`tests/`), skills (`skills/tw-blazor`)
**Plan / brief:** task.md `## Requirements` and `## Goal (implementer restatement)`: shared `components/MessageBar.razor` with the icon-only Copy button, shell host and four page-local call sites through it, `NotificationState.CopyMessageBar`, analyzer TWA0037, TWA0025 extended to the shared control
**Effort:** 3 (host Budget.ByDiff); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle Claude Fable 5.1 (`ganda task work`, headless print mode, 2026-10-30); `general` reviewer sub-agent Claude Sonnet (read-only, wrote `round-1/general.md`)

## Budget (by-diff)

- Lines changed: 292
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Gates re-run by the review oracle (round 1)

| Gate | Result |
|------|--------|
| `dev build` | 0 warnings, 0 errors |
| `web-spa-integration-tests --filter-class Copy` | 14/14 |
| `timewarp-architecture-analyzers-tests --filter-class MessageBar` | 13/13 |
| `timewarp-architecture-analyzers-tests --filter-class Message_Bars` | 15/15 |
| `ganda repo audit` | passes; 1 advisory (`kanban-landed`: 8 other tasks' claims, not 445) |
| `dev lint-skills` | 9/9 skills, eval.yaml valid |
| `dev template-smoke` (default variant) | SUCCEEDED (441/441, 9/9, 14/14 aggregators) |

Goal restatement vs task: no misread. The restatement adds that TWA0025 must also match the shared control; that is a consequence of the task's own "one shared control" requirement, not a divergence from it.

## Round 2 (fix-pass delta)

**Diff scope:** commit 1a7797596 (round-1 base ecfc574f0 → HEAD), 8 product/test files, 171+/80-.
**Effort:** 2 (host Budget.ByDiff, 292 lines); roster axes: general; turn cap 120.
**Session IDs:** review oracle Claude Fable 5.1 (`ganda task work`, headless print mode, 2026-10-30); `general` reviewer sub-agent Claude Sonnet (read-only, no builds, wrote `round-2/general.md`).

| Gate | Result |
|------|--------|
| `dev build` (`./bin/dev`) | 0 warnings, 0 errors |
| `web-spa-integration-tests --filter-class Copy` | 16/16 |
| `web-spa-integration-tests --filter-class DesignSystemPage` | 6/6 |
| `timewarp-architecture-analyzers-tests --filter-class MessageBar` | 16/16 |
| `timewarp-architecture-analyzers-tests --filter-class Message_Bars` | 15/15 |
| `ganda repo audit` | exit 0; 1 advisory (`kanban-landed`: 8 other tasks' claims, not 445) |
| `dev template-smoke` (default variant) | SUCCEEDED (441/441, 9/9, 14/14 aggregators) |

`dev lint-skills` not re-run: the delta does not touch `skills/`. Outcome: M1-M4 fixed, 0 new findings, 0 open → `disposition.md` clean.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
