# Review framework — task 104-028

**Date:** 2026-07-19
**Host task:** kanban/in-progress/104-028-add-optimistic-concurrency-token-to-identity-entities-and-store-port/
**Diff scope:** commit 85932b87 ("feat(identity): optimistic concurrency token on entities and store port (104-028)") vs its parent, on branch dev — 14 files
**Plan / brief:** task.md Notes "Implementation plan (2026-07-19)" — Entity<TId> adoption in timewarp-identity, rehydration ctor in foundation-domain, ConcurrencyConflictException + port conflict contract, in-memory store snapshot-on-get + Lock-guarded version check, race-test suite
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator 78b9f414-b92e-4554-9795-d8fa114bdb26; build agent a2ef2354b0fc976e7; reviewer a31739ea747756530

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
