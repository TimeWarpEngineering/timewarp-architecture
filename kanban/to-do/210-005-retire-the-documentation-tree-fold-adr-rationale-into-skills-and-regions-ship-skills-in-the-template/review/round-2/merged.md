# Round 2 — merged findings
**Date:** 2026-09-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/api/projects/api-server/program.cs:21
- Description: Design region cited deleted `how-to-agent-identity-host-split-web-vs-api.md`.
- Suggestion: Point at `agent-bearer-stores-module-infrastructure.cs` Design region.
- Source: general
- Disposition notes: Re-verified. Design region points at `agent-bearer-stores-module-infrastructure.cs`.

### M2 — Severity: bug — Status: fixed
- File: scripts/postgres/overview.md:51
- Description: Heading cited deleted `how-to-add-your-aggregate.md §8`.
- Suggestion: Retarget to `tw-aggregate-pattern` Schema evolution.
- Source: general
- Disposition notes: Re-verified. Heading retargeted; command block unchanged.

### M3 — Severity: bug — Status: fixed
- File: scripts/postgres/ef-shared-variables.ps1:8
- Description: Comment cited deleted `how-to-add-your-aggregate.md §8`.
- Suggestion: Replace with `tw-aggregate-pattern` (Schema evolution).
- Source: general
- Disposition notes: Re-verified.

### M4 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/template-smoke-harness.cs:714
- Description: Analysis exclusion only probed `skills/tw-web-api-contracts/analysis`.
- Suggestion: Enumerate any `analysis` directory under generated `skills/`.
- Source: general
- Disposition notes: Re-verified. `EnumerateDirectories(..., "analysis", AllDirectories)`; relative path in error; success gated on `ok`.

### M5 — Severity: suggestion — Status: wontfix
- File: .editorconfig:396-399
- Description: Local `directory-structure.severity = warning` does not drop `documentation/` from ganda `RequiredDirectories`.
- Suggestion: Prefer a ganda-side drop of `documentation/` from `RequiredDirectories`.
- Source: general
- Disposition notes: Re-verified wontfix. Local warning keeps audit non-blocking. RequiredDirectories lives in ganda. Decided by orchestrator.

### M6 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/SKILL.md:151
- Description: Public skill cited `(ADR-0010)` after ADR pages retired; EF mapping comments cited ADR-0009.
- Suggestion: Replace with skill / Design-region homes.
- Source: general
- Disposition notes: Re-verified. Skill points at `IPermissionEvaluator` Design region; named mapping comments cite `tw-aggregate-pattern`.

### M7 — Severity: nit — Status: fixed
- File: kanban/to-do/210-005-retire-the-documentation-tree-fold-adr-rationale-into-skills-and-regions-ship-skills-in-the-template/inventory.md:107
- Description: Counts said 5 unique skill destinations but listed four names.
- Suggestion: Change `5` to `4`.
- Source: general
- Disposition notes: Re-verified.

## Duplicates / conflicts

- None. No new IDs this round. No reopened IDs.
