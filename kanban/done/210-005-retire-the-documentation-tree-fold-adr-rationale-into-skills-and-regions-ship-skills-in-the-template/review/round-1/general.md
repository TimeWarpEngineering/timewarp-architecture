# Round 1 — general
**Date:** 2026-09-12
**Scope reviewed:** branch task/210-005-retire-the-documentation-tree-fold-adr-rationale-i vs origin/master

## Summary

The branch retires `documentation/` (76 markdown files + 4 companions, inventory rows match the deleted tree 1:1), folds still-true ADR/how-to rules into four skills and five Design-region homes, packs `skills/**` (excluding `**/analysis/**`) into the template, and rewrites `AGENTS.md` so Purpose/Design + skills are the documentation of record. Packaging, badges, parent-ledger M2/M20–M25/M27/M28, and folded public skill content check out. Residual risk is leftover call-site pointers to deleted how-tos and an analysis-exclude assertion that only probes one skill.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/api/projects/api-server/program.cs:21
- Description: Design region still says `see how-to-agent-identity-host-split-web-vs-api.md` after that page was deleted. The rule was folded into `agent-bearer-stores-module-infrastructure.cs`, but this surrounding call site was not retargeted.
- Suggestion: Point at the `agent-bearer-stores-module-infrastructure.cs` Design region (or inline the one-line host-split rule already there) and drop the deleted filename.
- Status: open

### Issue 2 — Severity: bug
- File: scripts/postgres/overview.md:51
- Description: Heading still cites `how-to-add-your-aggregate.md §8`, which was deleted and folded into `tw-aggregate-pattern` (Add an aggregate / Schema evolution).
- Suggestion: Retarget to `skills/tw-aggregate-pattern/SKILL.md` (Schema evolution / Canonical CLI) or drop the cross-reference and keep the local command block as SSOT here.
- Status: open

### Issue 3 — Severity: bug
- File: scripts/postgres/ef-shared-variables.ps1:8
- Description: Comment still cites `how-to-add-your-aggregate.md §8` for why web-server is the EF startup project. Same deleted page as Issue 2.
- Suggestion: Replace with `tw-aggregate-pattern` (Schema evolution) or state the rule locally without the deleted filename.
- Status: open

### Issue 4 — Severity: suggestion
- File: tools/dev-cli/services/template-smoke-harness.cs:714
- Description: `AssertSkillsShipped` correctly requires the eight `skills/*/SKILL.md` files, but the analysis exclusion only checks `skills/tw-web-api-contracts/analysis`. The pack exclude and success message claim `skills/*/analysis` broadly. Today only that one analysis directory exists, so smoke passes; a future `skills/<other>/analysis` would ship undetected by this assert.
- Suggestion: Enumerate `Directory.EnumerateDirectories(skillsDir, "analysis", SearchOption.AllDirectories)` (or equivalent) and fail if any analysis directory is present.
- Status: open

### Issue 5 — Severity: suggestion
- File: .editorconfig:396-399
- Description: `[ganda.audit] directory-structure.severity = warning` correctly keeps `ganda repo audit` non-blocking (exit 0; `directory-structure` reports Missing `documentation/` as advisory Warning). It does not remove `documentation/` from ganda `RequiredDirectories`, so every audit still fails that check at warning severity.
- Suggestion: Keep the local severity override for now (it works). Prefer a ganda-side drop of `documentation/` from `RequiredDirectories` (or a true per-repo exemption) so retiring the tree is not a permanent advisory FAIL.
- Status: open

### Issue 6 — Severity: suggestion
- File: skills/tw-web-api-contracts/SKILL.md:151
- Description: Public skill text still cites `(ADR-0010)` after ADR pages were retired. The enforcing home is now the `i-permission-evaluator-application.cs` Design region. Related orphaned ADR shorthand also remains in Design comments at `principal-entity-type-configuration-infrastructure.cs:10` and `profile-entity-type-configuration-infrastructure.cs:19` (`ADR-0009`).
- Suggestion: Replace ADR number citations with the skill or Design-region home (`tw-aggregate-pattern` / `IPermissionEvaluator` Design region).
- Status: open

### Issue 7 — Severity: nit
- File: kanban/to-do/210-005-retire-the-documentation-tree-fold-adr-rationale-into-skills-and-regions-ship-skills-in-the-template/inventory.md:107
- Description: Counts line says `fold-into-skill: 5 unique destinations` but lists four skill names (`tw-web-api-contracts`, `tw-feature-placement`, `tw-aggregate-pattern`, `tw-slice-isolation`). The “covering 7 files” count is correct.
- Suggestion: Change `5` to `4`.
- Status: open
