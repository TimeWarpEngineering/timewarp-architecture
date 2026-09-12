# Review framework — task 210-005

**Date:** 2026-09-12
**Host task:** kanban/to-do/210-005-retire-the-documentation-tree-fold-adr-rationale-into-skills-and-regions-ship-skills-in-the-template/
**Diff scope:** branch `task/210-005-retire-the-documentation-tree-fold-adr-rationale-i` vs `origin/master` (implementation commit `db730d04` plus merge `a3a0da1a`). Working tree clean.
**Plan / brief:** Retire `documentation/` (76 markdown files). Fold still-true ADR/how-to rules into skills or Design regions per `inventory.md`. Ship `skills/**` (excluding `**/analysis/**`) in the template. Rewrite `AGENTS.md` Documentation section. Fix `readme.md` badges. Close parent 210 findings M2, M20–M25, M27, M28.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok-4.6 (2026-09-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Reviewer brief

Check that the implementation matches the task requirements:

1. Inventory covers all 76 `documentation/` files with delete / fold-into-skill / fold-into-Design-region.
2. `documentation/` is gone; surviving rules actually landed in the named skill/region (not just deleted).
3. Template pack includes `skills/**` and excludes `skills/**/analysis/**`; `template.json` does not exclude `skills/` itself.
4. `AGENTS.md` Documentation section rewritten; former `documentation/` pointers have named new homes.
5. Folded skill content is public (no client names, no past-tense migration narrative).
6. `readme.md` badges and `runfiles/overview.md` (deleted) address M28.
7. Smoke assertion `AssertSkillsShipped` is complete enough (eight skills; analysis excluded).
8. Parent ledger M2, M20–M25, M27, M28 marked fixed only if the work actually closes them.

Do not re-litigate the human decision to retire `documentation/`. Review the fold-in and packaging, not the policy.

## Round 2

**Date:** 2026-09-12
**Scope:** re-verify M1–M7 against the post-fix uncommitted delta (plus surrounding call sites). Round 1 `merged.md` is frozen after this round opens. Carry stable M# IDs. New defects on the fix delta get new IDs.
**Roster:** general (effort 1)
