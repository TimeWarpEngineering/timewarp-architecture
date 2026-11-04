# Review framework

## Budget (by-diff)

- Lines changed: 147
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-04
**Host task:** kanban/in-progress/471-follow-the-os-color-scheme-on-first-visit-when-no-theme-mode-is-stored/
**Diff scope:** branch `task/471-follow-the-os-color-scheme-on-first-visit-when-no` (HEAD 8e45abfa7) vs merge-base with `master`
**Plan / brief:** task.md `## Goal (implementer restatement)`: BrandThemeScript sets the initial body[data-theme] from the stored mode, else the OS preference, before first paint, and never stores a mode
**Effort:** 2 (general only)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under ganda task work (session id is recorded on task.md by the host)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
