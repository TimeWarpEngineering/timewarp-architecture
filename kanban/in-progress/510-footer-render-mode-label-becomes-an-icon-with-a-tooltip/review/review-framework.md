# Review framework — task 510

**Date:** 2026-11-24
**Host task:** kanban/in-progress/510-footer-render-mode-label-becomes-an-icon-with-a-tooltip/
**Diff scope:** branch `task/510-footer-render-mode-label-becomes-an-icon-with-a` vs `master` (commit `74a99fefe` plus the kanban kitchen). Product files: `TimeWarpPage.razor`, `TimeWarpPage.razor.css`, `render-mode-badge.cs`, `tokens.css`, the TimeWarpPage and badge tests, the render-mode Playwright tests, and the footer selectors in the existing Playwright waits. Evidence PNGs under this kitchen's `evidence/`.
**Plan / brief:** task.md Description and Requirements. The implementer restatement matches that brief (icon map, muted colour, tooltip and aria-label, `data-qa` / `data-mode`, fade on a name change, isolated CSS, layout held, evidence shots).
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** implementer claude `bb9ffa58-5215-45a6-aed6-150b57118364`; reviewer grok `01a20c65-2815-71e2-b928-0a2724a1d1eb`

## Budget (by-diff)

- Lines changed: 425
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
