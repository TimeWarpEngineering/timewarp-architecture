# Review framework — task 135

**Date:** 2026-07-29
**Host task:** kanban/in-progress/135-adopt-co-located-jaribu-test-convention/
**Diff scope:** branch `Claude/2026-07-29/adopt-co-located-jaribu-tests` vs `dev` (4 commits, 14 files: grammar JSON + generator + regenerated g.cs/g.props ×3 families + analyzer tests; two ported co-located runfiles; template-smoke two-tier checks; docs)
**Plan / brief:** `../plan.md` — production adoption of the co-located Jaribu convention (MERGES, unlike the 134 spike)
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator c6f1a13b-487f-4085-bf61-ba4761e8579e

## Ground rules

- Reviewers read-only on product code; findings only under `review/round-N/`
- Severity bug | suggestion | nit; status starts open; zero issues is valid
- Verify falsifiable claims (build/test numbers) and run NEGATIVE probes (TWA0015 firing, guard integrity)
- Production standard — this branch merges
- Known/excluded: pre-existing `kebab-path-names` audit failures on dev (83 paths, unrelated)
