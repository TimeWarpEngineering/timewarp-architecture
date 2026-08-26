# Move merged 172 kitchen into kanban done so the origin-home board matches PR 298

## Description

Architecture **172** (restore GlobalUsingsAnalyzer; style warning/none) already merged as
PR **298** (2026-08-12). The kitchen is still
`kanban/in-progress/172-restore-globalusingsanalyzer-style-rules-warning-or-none-no-suggestion-noise.md`
on origin-home, so `ganda reposet show live` lists it as in-progress.

Open checklist items on 172 are **follow-ups**, not unfinished 172: residual IDE0005 sweep,
GlobalUsingsAnalyzer0003 + inside_namespace, XML docs **177**, githooks **203**.

## Requirements

Kanban-only. Do not change editorconfig, CPM, or other product files.

1. `git mv` **172** from `kanban/in-progress/` to `kanban/done/`. Kitchen already has
   Results; do not rewrite the product story. Leave follow-up checklist items unchecked
   (they are other tasks).
2. This task itself must be in `kanban/done/` in the PR, with Results + How to validate.
3. PR; STOP. Do not merge.

Do not re-id 172. Do not implement 177. Do not touch other in-progress kitchens.

## Checklist

- [x] Move 172 kitchen to `kanban/done/`
- [x] This kitchen in `done/`; PR; STOP

## Session

- Created: 1429125 (2026-08-26)
- Cockpit: Grok 01a0275a — Live still showed 172 in-progress after PR 298 merged
- Implementation: `git mv` 172 `kanban/in-progress/` → `kanban/done/` (body unchanged; follow-up items left unchecked) (2026-08-26)
- Board: `ganda kanban done 204` (claim + worktree remain for PR)

## Notes

Same pattern as flow **116** / architecture **202**.
- Implementer launch: host=herdr profile=implementer-grok provider=grok worktree=/home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-architecture/task-204-move-merged-172-kitchen-into-kanban-done-so-the-or workspace=w12 pane=w12:p1 agent=task204 (2026-08-26 UTC)

## Results

Kanban-only. `git mv` architecture **172** from `kanban/in-progress/` to `kanban/done/` so the origin-home board matches merged PR **298**. Did not rewrite 172's product story. Leftover 172 checklist items stay unchecked (residual IDE0005 sweep, GlobalUsingsAnalyzer0003 + `inside_namespace`, XML docs **177**, githooks **203**). No editorconfig, CPM, or other product files changed.

### How to validate

**Smoke**

```bash
test ! -e kanban/in-progress/172-restore-globalusingsanalyzer-style-rules-warning-or-none-no-suggestion-noise.md && echo no-in-progress-172
# Expect: no-in-progress-172

test -f kanban/done/172-restore-globalusingsanalyzer-style-rules-warning-or-none-no-suggestion-noise.md && echo ok-172
# Expect: ok-172

ganda kanban path 172
# Expect: …/kanban/done/172-restore-globalusingsanalyzer-style-rules-warning-or-none-no-suggestion-noise.md

test -f kanban/done/204-move-merged-172-kitchen-into-kanban-done-so-the-origin-home-board-matches-pr-298.md && echo ok-204
# Expect: ok-204

ganda kanban path 204
# Expect: …/kanban/done/204-move-merged-172-kitchen-into-kanban-done-so-the-origin-home-board-matches-pr-298.md

git diff origin/master...HEAD --stat
# Expect: only kanban/ paths (172 column move + 204 kitchen)
```

**Expect**

- `ganda reposet show live` / `ganda kanban` do not list architecture 172 as in-progress.
- 172 stays id **172** in `kanban/done/` with original Results and unchecked follow-up items.
- This kitchen is in `kanban/done/` on the PR branch. No product code in the diff.
