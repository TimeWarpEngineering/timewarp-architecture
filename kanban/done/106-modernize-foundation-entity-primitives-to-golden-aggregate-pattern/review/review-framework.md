# Review framework — task 106

**Date:** 2026-07-19
**Host task:** kanban/in-progress/106-modernize-foundation-entity-primitives-to-golden-aggregate-pattern/
**Diff scope:** commit 437f0e17 ("feat(foundation): modernize entity primitives to golden aggregate pattern (106)") vs its parent, on branch dev — 41 files
**Plan / brief:** task.md Notes "Implementation plan (2026-07-19)" — Entity<TId> + IAggregateRoot primitives, Profile/ProfileId exemplar, DomainInvariantsGuard + PostgresDbContext SaveChanges hook, TWA0011/0012 analyzer, nopCommerce sketch deletion, ValueObject/BaseEvent removal, 3 new/extended test projects
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator 78b9f414-b92e-4554-9795-d8fa114bdb26; build agent a2ef2354b0fc976e7

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
