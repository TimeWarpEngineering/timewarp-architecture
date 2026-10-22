# Review framework

## Budget (by-diff)

- Lines changed: 351
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-22
**Host task:** kanban/to-do/379-001-upstream-pageheader-and-sectionheader-from-crunchit-portal/
**Diff scope:** branch `task/379-001-upstream-pageheader-and-sectionheader-from-crunchi` at
`a4d5ff241` vs merge-base `5742efb7c` (master). 8 product files under `source/` and `tests/`.
**Plan / brief:** task.md Description, Checklist, and `## Goal (implementer restatement)`.
TimeWarpPage gains `Description` and `Actions` slots. SectionHeader is a new composite. The
style guide, todo, and feedback pages adopt them, and there are HtmlRenderer render tests.
**Effort:** 2. **Reviewer roster:** general (review oracle, claude-opus-5-5).
**Session IDs:** review oracle session not reported by the host.

### Round 2

**Diff scope:** fix delta `aac996375..fee49aa3b`. It re-verifies round-1 M1–M4 and scans the
delta for new defects. **Effort:** 2. **Roster:** general (review oracle, claude-opus-5-5).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
