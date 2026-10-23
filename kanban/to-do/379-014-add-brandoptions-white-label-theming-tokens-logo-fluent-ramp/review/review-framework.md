# Review framework

## Budget (by-diff)

- Lines changed: 843
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope (review oracle)

**Date:** 2026-10-24
**Host task:** kanban/to-do/379-014-add-brandoptions-white-label-theming-tokens-logo-fluent-ramp/
**Diff scope:** commit 305c57984 (`feat(web-spa): brand the shell from BrandOptions`) against its parent f34ccbe4c, `source/` and `tests/` only. Earlier commits on the branch belong to 379-012 and 379-013, which were already reviewed.
**Plan / brief:** `## Goal (implementer restatement)` in task.md. BrandOptions on both hosts, `/brand.css`, the logo from options, the Fluent seed on the body and a pre-Blazor storage script, a style-guide card, and tests.
**Effort:** 3, general axis
**Reviewer roster:** general. The review oracle (claude-opus-5-5) did one pass. An independent general-purpose subagent (claude-opus-5-5) did a second pass without seeing the oracle's conclusions. The two passes are merged into `round-N/general.md`.
**Session IDs:** review oracle ea832021-9579-4f4a-b67f-01532e447553. Subagent a94ae757bf634b8e4.

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Address the diff and the surrounding call sites. Re-verify falsifiable claims against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.
