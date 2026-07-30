# Round 2 — merged findings (re-review of fix commit 20646757)
**Date:** 2026-07-29
**Sources:** general (round 1) + orchestrator re-verification (round 2)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- Enforcement-surface clarification. Verified on branch: AGENTS.md line ~153 ("this only
  fires when the file itself is compiled — standalone…"), same sentence in
  documentation/developer/standards/file-naming.md (~line 34) and
  skills/tw-feature-placement/SKILL.md; task-136 aggregator note included.

### M2 — Severity: nit — Status: fixed
- Runfile modes. Verified `git ls-tree`: both `-tests.cs` files 100755; implementer
  spot-checked direct `./file.cs` execution (5/5).

### M3 — Severity: suggestion — Status: fixed
- Dead-end tw-jaribu pointer. Verified: canonical preamble inlined in
  skills/tw-feature-placement/SKILL.md new section "Co-located Jaribu runfile preamble"
  (including the corrected `NoWarn=$(NoWarn);…` prefix form); AGENTS.md + file-naming.md
  repointed there; cross-repo tw-jaribu update noted as follow-up.

## Re-review scope note

Round 2 was an orchestrator verification of the three mechanical fixes (git ls-tree modes,
doc content greps on the branch, `dev build` 0/0 reported by implementer). No new findings.
