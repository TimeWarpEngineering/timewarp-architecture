# Review framework

## Budget (by-diff)

- Lines changed: 415
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework (round 1)

**Date:** 2026-10-30
**Host task:** kanban/in-progress/446-ask-finds-ctrl-k-deeplinks-in-site-map-and-navigate/
**Diff scope:** commit dc4e009e4 (`feat(agent): site_map and navigate list Ctrl-K design-system deep links`) vs its parent 892eb484d; product paths under `source/`, `skills/`, `tests/`
**Plan / brief:** task.md `## Goal (implementer restatement)` and checklist: site_map, navigate and the Ask summary list the design-system part deep links Ctrl-K lists, gated on the Design System page being permitted, from one source (`CommandPaletteRoster.DeepLinks`)
**Effort:** 2 (by-diff, 415 lines); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle Claude Fable 5.1 (claude-fable-5-1) under `ganda task work` review node, 2026-10-30

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
