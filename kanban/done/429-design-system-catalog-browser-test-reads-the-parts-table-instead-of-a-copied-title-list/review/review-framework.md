# Review framework

## Budget (by-diff)

- Lines changed: 255
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-28
**Host task:** kanban/in-progress/429-design-system-catalog-browser-test-reads-the-parts-table-instead-of-a-copied-title-list/
**Diff scope:** branch `task/429-design-system-catalog-browser-test-reads-the-parts` (HEAD 2420bbce4) vs merge base 7a70a183a with master
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: derive expected part titles and group headings in the catalog tests from `DesignSystemParts`
**Effort:** 2 (budget by diff); roster axes general only
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, ganda task work headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
