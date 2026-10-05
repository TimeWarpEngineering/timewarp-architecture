# Review framework

## Budget (by-diff)

- Lines changed: 3014
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 275

**Date:** 2026-10-05
**Host task:** kanban/to-do/275-hypermedia/
**Diff scope:** branch `task/275-hypermedia` vs `master` (merge-base), commits 18f4a6be..70c3b3c8; 35 files, +2984/-30
**Plan / brief:** task.md Requirements 1–9 — hypermedia lab slice (approach B catalog-named offers, approach C Siren-style links), Ctrl-K contextual rows hook, shared `CredentialRules`, B-vs-C comparison write-up
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general, security, tests (specialists chosen for effort 3: C follows server hrefs with the bearer token; contextual rows are an allow-list)
**Session IDs:** review oracle (Claude Opus 5.5, headless ganda task work); reviewers are subagents of this session

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- This is a lab/evaluation: the losing approach and lab page are removed before shipping; review for correctness and rule compliance, not long-term polish
