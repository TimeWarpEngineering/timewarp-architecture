# Review framework

## Budget (by-diff)

- Lines changed: 2350
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 424

**Date:** 2026-10-28
**Host task:** kanban/in-progress/424-style-guide-page-header-example-xai-key-policy-on-the-ai-page-nav-stays-open-on-sub-routes/
**Diff scope:** branch `task/424-style-guide-page-header-example-xai-key-policy-on` at
`0d475995b` vs merge-base `aea778347` with master (commits 95858acc5, e4fa8ccc8, 623201076).
**Plan / brief:** task.md Description items 1–7 and `## Goal (implementer restatement)`.
**Effort:** 3. Three general reviewers, each on one slice of the diff.
**Reviewer roster:**
- general-1: xAI key policy move (item 2)
- general-2: nav match from the page registry and design-system parts in Ctrl-K (items 3, 5)
- general-3: Page header part, compact catalog, breadcrumb focus ring, rename (items 1, 4, 6, 7)
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work); reviewer subagents
abfcef1c21f175cfa, a1e79616ebde5ed6a, a5aa812952a2f275c.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
