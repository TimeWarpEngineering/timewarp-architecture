# Review framework — task 134

**Date:** 2026-07-29
**Host task:** kanban/in-progress/134-spike-jaribu-co-located-integration-tests/
**Diff scope:** branch `spike/134-jaribu-co-located-integration-tests` vs base `dev` (3 commits, 7 files: two co-located Jaribu runfiles, web+api feature-membership.targets carve-out, JARIBU_MULTI aggregator project + global.json, Directory.Packages.props pin)
**Plan / brief:** `../plan.md` — spike proving Jaribu co-located tests (contracts round-trip + real-host integration test), membership-guard carve-out, `dotnet test` discovery. Spike code may never merge; findings.md is the deliverable of record.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator c6f1a13b-487f-4085-bf61-ba4761e8579e

## Ground rules

- Reviewers are read-only on product code; findings go only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
  (the implementation report claims: 5/5 + 2/2 standalone passes, 7/7 via `dotnet test`,
  `dev build` 0/0 with `-tests.cs` files present — verify, don't trust)
- Review proportionate to a SPIKE: correctness of evidence and conventions that would mislead
  the follow-up adoption tasks matter; gold-plating throwaway code does not
- Prior rounds are immutable; new work goes in `round-(N+1)/`
