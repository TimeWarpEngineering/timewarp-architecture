# Review framework

## Budget (by-diff)

- Lines changed: 3262
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 345

**Date:** 2026-10-13
**Host task:** kanban/to-do/345-ask-references-site-wide-typed-resource-references-backed-by-list-actions/
**Diff scope:** branch `task/345-ask-references-site-wide-typed-resource-references` vs `master` (commit 7e85ae699)
**Plan / brief:** task.md Design direction + Results (ReferenceableResource marker, TWA0033/TWA0034 analyzer, registry generator, AskReferenceState + picker, Search on list contracts, old page-scoped menu removed)
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (session not reported); general reviewer subagent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
