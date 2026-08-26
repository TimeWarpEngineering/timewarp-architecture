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

- [ ] Move 172 kitchen to `kanban/done/`
- [ ] This kitchen in `done/`; PR; STOP

## Session

- Created: 1429125 (2026-08-26)
- Cockpit: Grok 01a0275a — Live still showed 172 in-progress after PR 298 merged

## Notes

Same pattern as flow **116** / architecture **202**.
