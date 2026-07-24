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

- [ ] Semantic rename (prefer Roslynk rename_symbol): `GoldenDbContext` → `AggregateDbContext`,
      `GoldenAggregateVersionConvention` → `AggregateVersionConvention` (namespace stays
      `TimeWarp.Foundation.Persistence`)
- [ ] File renames (kebab-case follows type): `golden-db-context.cs` → `aggregate-db-context.cs`,
      `golden-aggregate-version-convention.cs` → `aggregate-version-convention.cs`,
      `golden-db-context-tests.cs` → `aggregate-db-context-tests.cs`
- [ ] Prose sweep in code comments/Design regions: "golden hook/pin/convention/enforcement" →
      aggregate-describing language ("the aggregate SaveChanges hook", "the Version convention").
      Files (from survey): postgres-db-context.cs, ef-principal-store.cs, profile + principal
      entity-type-configuration files, domain-invariants-guard.cs,
      missing-invariants-validator-exception.cs, entity.cs, entity-version.cs,
      web-domain/aggregates/overview.md, plus the two renamed foundation files
- [ ] Docs sweep: ADR-0009 + HowToAddYourAggregate + both Overview.md — update type-name
      references; "golden path" prose may stay where it means "the recommended path"
      (ADR-0009's title/filename stays — it is a historical record and the idiom is fine)
- [ ] Do NOT rewrite kanban done/ records or review files — they are historical
- [ ] dev build 0/0, foundation-infrastructure-tests, web-infrastructure-tests,
      web-server-integration-tests, dev template-smoke — all green

## Notes

Origin: naming conversation 2026-07-24 — "I used the term Golden to mean high quality
excellence... I did NOT mean for you to name stuff GoldenXYZ." The names came from echoing
the user's vocabulary, not from naming discipline; repo convention already bans
meaning-free names (no grab-bag namespaces).

Sequencing: blocks the publish-residuals task (republish Foundation/Attributes, first-publish
TimeWarp.Identity). Cheap now, breaking later.
