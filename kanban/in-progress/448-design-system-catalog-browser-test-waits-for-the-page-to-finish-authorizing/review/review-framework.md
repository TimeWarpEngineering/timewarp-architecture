# Review framework

## Budget (by-diff)

- Lines changed: 287
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 448

**Date:** 2026-10-30
**Host task:** kanban/in-progress/448-design-system-catalog-browser-test-waits-for-the-page-to-finish-authorizing/
**Diff scope:** commit fd411af74 (parent ef4f86a8b) on branch `task/448-design-system-catalog-browser-test-waits-for-the-p`; product files `tests/container-apps/web/web-spa-playwright-tests/{playwright-page-load.cs, design-system-catalog-playwright-tests.cs, breadcrumb-playwright-tests.cs, clear-xai-api-key-playwright-tests.cs}`
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Results`: one shared `PlaywrightPageLoad` helper that loads a document and waits up to 30 s for the shell app bar, used by the catalog, breadcrumb, and clear-key tests.
**Effort:** 2 (host Budget.ByDiff), general axis only
**Reviewer roster:** general (reviewer-claude, model claude-fable-5-1); one read-only scan sub-agent for the other full-page loads
**Session IDs:** review oracle session recorded by the host in `oracle-review-*.log`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
