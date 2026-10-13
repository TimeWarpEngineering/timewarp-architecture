# Resolve duplicate kanban id 001 in done (ganda repo audit `kanban-duplicate-ids`)

## Problem

`ganda repo audit` now fails `kanban-duplicate-ids: Duplicate kanban task id(s): 001`. That blocked
`ganda pr merge 477` (merged with `--skip-audit`, since the failure is unrelated to that PR). It
will block every merge until it's fixed.

Two historical done tasks share id 001 (both arrived via `ea6df20ff`, 2026-02-26, "move kanban to repo
root and rename to kebab-case"):
- `kanban/done/001-navlink-encapsulation-implementation.md`
- `kanban/done/001-fix-fastendpointsourcegenerator.md`

## Requirements

- Keep one as 001 (the older by content or git history; record which and why). Give the other a
  fresh id from **`ganda kanban reserve`**. Never hand-number. Rename the file to the new id, with
  the same slug.
- Update any references to the renamed task: grep `kanban/`, `skills/`, and source Design regions
  for the old filename and for "task 001" mentions that mean the renamed one.
- `ganda repo audit` passes (no `kanban-duplicate-ids`).
- Kanban-only change; no code.

## Checklist

- [ ] Reserve new id, rename one file, fix references
- [ ] `ganda repo audit` clean

## Session

- Created: 2026-10-13 (cockpit; found when `ganda pr merge 477` was refused)
