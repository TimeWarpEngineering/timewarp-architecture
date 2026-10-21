# Review framework

## Budget (by-diff)

- Lines changed: 1658
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-21
**Host task:** kanban/to-do/317-deploy-target-aca-neon-free-tier-azure-container-apps-with-neon-postgres/
**Diff scope:** branch `task/317-deploy-target-aca-neon-free-tier-azure-container-a` vs `master` (`git diff master...HEAD`, 4 commits, head 8727f5888), kanban paths excluded
**Plan / brief:** task.md Requirements, Acceptance criteria, and `## Goal (implementer restatement)`
**Effort:** 3
**Reviewer roster:** general (review oracle, Opus 5.5) plus one general sub-reviewer (Sonnet). Findings merged and re-verified by the oracle.
**Session IDs:** review oracle (claude-opus-5-5, headless `ganda task work`), session id not exposed; sub-reviewer agent a270e187bf5cb516b

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
