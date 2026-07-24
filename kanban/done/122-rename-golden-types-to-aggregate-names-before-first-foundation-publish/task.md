# Rename Golden types to aggregate names before first Foundation publish

## Description

"Golden" leaked from conversation shorthand ("the golden path" = the recommended, exemplary
way) into public type names, where it communicates nothing about behavior — a quality
adjective frozen into API. Steve's call (2026-07-24): rename to behavior-describing names.

| Current | New |
|---------|-----|
| `GoldenDbContext` | `AggregateDbContext` |
| `GoldenAggregateVersionConvention` | `AggregateVersionConvention` |

Rule going forward: **"golden path" stays legal as prose idiom** (ADRs, how-tos describing
the recommended route); **"Golden" as a type/member/file name prefix is banned** — names must
describe what the thing does.

**MUST land before the Foundation publish residuals task runs**: TimeWarp.Foundation.Infrastructure
has never been published with these types, so the rename is free today and a breaking API
change the day after first publish.

## Checklist

- [x] Semantic rename (prefer Roslynk rename_symbol): `GoldenDbContext` → `AggregateDbContext`,
      `GoldenAggregateVersionConvention` → `AggregateVersionConvention` (namespace stays
      `TimeWarp.Foundation.Persistence`)
- [x] File renames (kebab-case follows type): `golden-db-context.cs` → `aggregate-db-context.cs`,
      `golden-aggregate-version-convention.cs` → `aggregate-version-convention.cs`,
      `golden-db-context-tests.cs` → `aggregate-db-context-tests.cs`
- [x] Prose sweep in code comments/Design regions: "golden hook/pin/convention/enforcement" →
      aggregate-describing language ("the aggregate SaveChanges hook", "the Version convention").
      Files (from survey): postgres-db-context.cs, ef-principal-store.cs, profile + principal
      entity-type-configuration files, domain-invariants-guard.cs,
      missing-invariants-validator-exception.cs, entity.cs, entity-version.cs,
      web-domain/aggregates/overview.md, plus the two renamed foundation files
- [x] Docs sweep: ADR-0009 + HowToAddYourAggregate + both Overview.md — update type-name
      references; "golden path" prose may stay where it means "the recommended path"
      (ADR-0009's title/filename stays — it is a historical record and the idiom is fine)
- [x] Do NOT rewrite kanban done/ records or review files — they are historical
- [x] dev build 0/0, foundation-infrastructure-tests, web-infrastructure-tests,
      web-server-integration-tests, dev template-smoke — all green

## Notes

Origin: naming conversation 2026-07-24 — "I used the term Golden to mean high quality
excellence... I did NOT mean for you to name stuff GoldenXYZ." The names came from echoing
the user's vocabulary, not from naming discipline; repo convention already bans
meaning-free names (no grab-bag namespaces).

Sequencing: blocks the publish-residuals task (republish Foundation/Attributes, first-publish
TimeWarp.Identity). Cheap now, breaking later.

### Implementation plan (Phase 2, 2026-07-24)

**No strategic ambiguity** — pure rename before first Foundation publish. No RFC.

| Step | Action |
|------|--------|
| 1 | Rename symbols: `GoldenDbContext` → `AggregateDbContext`, `GoldenAggregateVersionConvention` → `AggregateVersionConvention` (namespace unchanged) |
| 2 | Rename files kebab-case to match types; test file + Fixie namespace `GoldenDbContext_` → `AggregateDbContext_` |
| 3 | Prose in Design/Purpose: "golden hook/convention" → aggregate-describing language; keep "golden path" only as recommended-route idiom in docs |
| 4 | Docs: ADR-0009 type-name refs + HowToAddYourAggregate + Overview if needed; ADR title/filename stay |
| 5 | Skip kanban historical records |
| 6 | Verify: `dev build` 0/0; foundation-infrastructure-tests; web-infrastructure-tests; template-smoke if cheap |

## Results

**Completed 2026-07-24** — public Foundation type names describe aggregate behavior; rename
lands free before first publish.

### What was implemented
| Old | New |
|-----|-----|
| `GoldenDbContext` | `AggregateDbContext` |
| `GoldenAggregateVersionConvention` | `AggregateVersionConvention` |

- File renames (kebab-case + tests); namespace `TimeWarp.Foundation.Persistence` unchanged
- Prose/Design regions: aggregate-describing language
- Docs: ADR-0009 type refs + HowToAddYourAggregate; "golden path" idiom kept; ADR filename stays
- Historical kanban untouched

### Tests
- `dev build` 0/0
- foundation-infrastructure-tests: **11 passed**
- web-infrastructure-tests: **39 passed**
- web-server-integration-tests: **97 passed**, 1 skipped
- `dev template-smoke`: **SUCCEEDED** (SmokeDefault + SmokeNoPostgres)

### Phase 4b
- Effort 1 (general); 1 round; **0 open**
- Disposition: **clean** (`review/disposition.md`)

### Commit
- `a7cb2977` refactor(foundation): rename GoldenDbContext to AggregateDbContext

### Rule going forward
- **"golden path"** OK as recommended-route prose
- **`Golden*` type/member/file names banned**

## Session

- Created: 2026-07-24
- Plan: 2026-07-24 (orchestrator; task-as-spec, no open forks)
- Implemented + review: 2026-07-24
