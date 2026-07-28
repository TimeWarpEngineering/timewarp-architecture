# Review framework — task 126-011

**Date:** 2026-07-27
**Host task:** kanban/in-progress/126-011-…/
**Diff scope:** commits `351959b5` (seam moves, abstractions retired) + `c7d31c07` (placement
guide: skill opening, AGENTS.md compression)
**Plan / brief:** task.md is the spec; binding constraints: seams beside impls in clusters,
namespaces unchanged, one-sentence rule + litmus test verbatim, public-skill style, sanity test
must reproduce real placements, web-infrastructure-module.cs NOT moved (maintainer ruling
pending).
**Effort:** 1 (general, empirical) + orchestrator gate verification
**Reviewer roster:** general
**Session IDs:** orchestrator Claude Fable; implementer + reviewer Claude Sonnet subagents.

## Gate status (orchestrator-verified, uncontended re-run)

`dev build` 0/0 · `dev test` 15 projects 0 failed · `dev template-smoke` SmokeDefault OK +
SmokeNoPostgres OK.

## Ground rules

- Reviewer read-only on product code; writes only under `review/round-1/`
- Severity bug|suggestion|nit; zero issues is a valid outcome
- Empirical spot-check expected (repo standard for structure/docs changes: verify claims
  against the tree, run at least one cheap check)
