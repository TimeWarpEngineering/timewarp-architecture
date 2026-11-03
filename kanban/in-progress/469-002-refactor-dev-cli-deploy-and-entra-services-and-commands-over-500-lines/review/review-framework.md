# Review framework — task 469-002

**Date:** 2026-11-04
**Host task:** kanban/in-progress/469-002-refactor-dev-cli-deploy-and-entra-services-and-commands-over-500-lines/
**Diff scope:** branch `task/469-002-refactor-dev-cli-deploy-and-entra-services-and-com` vs merge-base `f95c82ea0` (master). 94 files, +7405 / −6483.
**Plan / brief:** Split the eight over-limit dev-cli deploy, entra, and template-smoke files into one top-level type per file, each at most 500 lines, without changing what any `dev` verb does.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** grok review `01a1a2ea-60e5-7ee3-b154-2ef4e8c7dd68`

## Budget (by-diff)

- Lines changed: 13888
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Goal check

The implementer restatement matches the original task: the same eight files, one top-level type per file, 500-line cap, thin endpoints, unchanged verb behavior, and Amuru wrappers around real tools. The restatement adds explicit `dev build` and `ganda repo audit` gates. Those tighten the original "tests still pass" and "AOT self-install still builds" requirements. They do not drop or retarget the work. No misread.
