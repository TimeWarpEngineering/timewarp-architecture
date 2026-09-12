# Round 1 — merged findings
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
- Description: Design region still says `see how-to-agent-identity-host-split-web-vs-api.md` after that page was deleted. The rule was folded into `agent-bearer-stores-module-infrastructure.cs`, but this surrounding call site was not retargeted.
- Suggestion: Point at the `agent-bearer-stores-module-infrastructure.cs` Design region (or inline the one-line host-split rule already there) and drop the deleted filename.
- Source: general
- Disposition notes: Design region now points at `agent-bearer-stores-module-infrastructure.cs`. Deleted filename removed.

### M2 — Severity: bug — Status: fixed
- File: scripts/postgres/overview.md:51
- Description: Heading still cites `how-to-add-your-aggregate.md §8`, which was deleted and folded into `tw-aggregate-pattern` (Add an aggregate / Schema evolution).
- Suggestion: Retarget to `skills/tw-aggregate-pattern/SKILL.md` (Schema evolution / Canonical CLI) or drop the cross-reference and keep the local command block as SSOT here.
- Source: general
- Disposition notes: Heading retargeted to `tw-aggregate-pattern` Schema evolution. Command block unchanged.

### M3 — Severity: bug — Status: fixed
- File: scripts/postgres/ef-shared-variables.ps1:8
- Description: Comment still cites `how-to-add-your-aggregate.md §8` for why web-server is the EF startup project. Same deleted page as M2.
- Suggestion: Replace with `tw-aggregate-pattern` (Schema evolution) or state the rule locally without the deleted filename.
- Source: general
- Disposition notes: Comment cites `tw-aggregate-pattern` (Schema evolution).

### M4 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/template-smoke-harness.cs:714
- Description: `AssertSkillsShipped` correctly requires the eight `skills/*/SKILL.md` files, but the analysis exclusion only checks `skills/tw-web-api-contracts/analysis`. The pack exclude and success message claim `skills/*/analysis` broadly. Today only that one analysis directory exists, so smoke passes; a future `skills/<other>/analysis` would ship undetected by this assert.
- Suggestion: Enumerate `Directory.EnumerateDirectories(skillsDir, "analysis", SearchOption.AllDirectories)` (or equivalent) and fail if any analysis directory is present.
- Source: general
- Disposition notes: Enumerates any `analysis` directory under generated `skills/` and fails if present.

### M5 — Severity: suggestion — Status: wontfix
- File: .editorconfig:396-399
- Description: `[ganda.audit] directory-structure.severity = warning` correctly keeps `ganda repo audit` non-blocking (exit 0; `directory-structure` reports Missing `documentation/` as advisory Warning). It does not remove `documentation/` from ganda `RequiredDirectories`, so every audit still fails that check at warning severity.
- Suggestion: Keep the local severity override for now (it works). Prefer a ganda-side drop of `documentation/` from `RequiredDirectories` (or a true per-repo exemption) so retiring the tree is not a permanent advisory FAIL.
- Source: general
- Disposition notes: Local warning is the intended remedy in this repo. Dropping `documentation/` from `RequiredDirectories` is a ganda-repo change, not this template. Advisory `directory-structure` warning accepted until ganda ships an exemption. Decided by orchestrator.

### M6 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/SKILL.md:151
- Description: Public skill text still cites `(ADR-0010)` after ADR pages were retired. The enforcing home is now the `i-permission-evaluator-application.cs` Design region. Related orphaned ADR shorthand also remains in Design comments at `principal-entity-type-configuration-infrastructure.cs:10` and `profile-entity-type-configuration-infrastructure.cs:19` (`ADR-0009`).
- Suggestion: Replace ADR number citations with the skill or Design-region home (`tw-aggregate-pattern` / `IPermissionEvaluator` Design region).
- Source: general
- Disposition notes: Skill points at `IPermissionEvaluator` Design region; both named EF mapping comments cite `tw-aggregate-pattern`.

### M7 — Severity: nit — Status: fixed
- File: kanban/to-do/210-005-retire-the-documentation-tree-fold-adr-rationale-into-skills-and-regions-ship-skills-in-the-template/inventory.md:107
- Description: Counts line says `fold-into-skill: 5 unique destinations` but lists four skill names (`tw-web-api-contracts`, `tw-feature-placement`, `tw-aggregate-pattern`, `tw-slice-isolation`). The “covering 7 files” count is correct.
- Suggestion: Change `5` to `4`.
- Source: general
- Disposition notes: Count corrected to 4.

## Duplicates / conflicts

- None. M2 and M3 share a deleted page (`how-to-add-your-aggregate.md`) but are distinct call sites.
