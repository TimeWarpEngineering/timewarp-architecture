# Review framework — task 515

**Date:** 2026-11-24
**Host task:** kanban/in-progress/515-extract-shared-interactive-html-renderer-test-helper-and-document-combined-test-filters/
**Diff scope:** branch `task/515-extract-shared-interactive-html-renderer-test` vs merge-base `f47562750` (master). Product commit `0e6cf10ab`. Kanban move and land commits are in the same range. The branch is 2 commits behind master; those commits only touch other kanban cards.
**Plan / brief:** Extract one shared `InteractiveHtmlRenderer` plus `InvokeAfterRenderAsync` for `web-spa-integration-tests`, point the three page-draft tests at it, and document one-run class/method filters in `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`.
**Effort:** 2 (general only; by-diff budget, no specialist axes)
**Reviewer roster:** general
**Session IDs:** grok `01a20d5d-fea3-7ce1-b3e0-f5422a7cff3b` (review). Implementer session is on `task.md`.

## Budget (by-diff)

- Lines changed: 318
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
