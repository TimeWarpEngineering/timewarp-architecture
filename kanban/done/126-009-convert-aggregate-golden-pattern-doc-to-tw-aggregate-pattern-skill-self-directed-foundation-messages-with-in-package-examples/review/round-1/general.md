# Round 1 — general

**Date:** 2026-07-27
**Scope reviewed:** commits `42f0808a` (skill + doc retirement + referrers) and `07f0c11f`
(foundation messages + XML examples), plus current repo state, against
`kanban/in-progress/126-009-…/task.md`.

## Summary

Both commits deliver the spec faithfully. `skills/tw-aggregate-pattern/SKILL.md` carries over
every substantive rule from the retired `web-domain/aggregates/overview.md` (typed id,
`Entity<TId>` base, fail-closed `Create`, named mutations, private nested `Invariants`,
save-time `DomainInvariantsGuard` enforcement including the complementary-not-redundant framing
and child→root resolution, the `Version`/`IsConcurrencyToken()` convention detail), written in
present tense with no task numbers or history narration, and matches the frontmatter/heading
style of `tw-feature-placement`/`tw-slice-isolation`. `web-domain/` is reduced to csproj +
`global-usings.cs` in git (confirmed via `git ls-files`). A repo-wide grep for
`aggregates/overview` returns zero hits outside `kanban/` (all in `done/` history and the host
task's own files). `HowToAddYourAggregate.md`'s three references, ADR-0009's pointer, and the
AGENTS.md callout all read correctly and point at the new skill.

The three rewritten foundation message strings state the fix inline (declare a private nested
`Invariants : AbstractValidator<T>`, TWA0011) with zero file paths — grepped both touched files
for `web-domain`, `features/`, `.md`, `overview`: no hits. The docs-site URL is correctly omitted;
I independently fetched `https://timewarpengineering.github.io/timewarp-architecture/` and found
no page about the aggregate pattern, consistent with the implementer's recorded evidence. The new
XML `<example>` blocks on `IAggregateRoot` and `Entity<TId>` are identical worked examples (a
minimal `Order` aggregate: private ctor + static `Create` with guard clause + named `Rename`
mutation + private nested `Invariants`) that verifiably satisfy TWA0011 (nested validator present)
and TWA0012 (it's private), and don't contradict the real exemplar
(`web/features/profile/profile-domain.cs`). `dotnet build -warnaserror` on `foundation-domain`
alone is clean (0 warnings), confirming the XML is well-formed and generics are properly escaped
(`&lt;OrderId&gt;`, etc.).

Design/Purpose regions on all four touched foundation files were reconciled — none still describe
the old path-pointing behavior; `missing-invariants-validator-exception.cs`'s Design region now
explicitly narrates the self-directed-messages rationale.

Empirical spot-checks:
- `dotnet fixie tests/foundation/foundation-application-tests` → **13 passed**
- `dotnet fixie tests/foundation/foundation-domain-tests` → **37 passed**
- `dotnet build -warnaserror` (foundation-domain project) → 0 warnings, 0 errors
- Assertion-survival claim verified: `tests/foundation/foundation-infrastructure-tests/aggregate-db-context-tests.cs:127-128` asserts `ex.Message.ShouldContain(nameof(IAggregateRoot))` and `ex.Message.ShouldContain(nameof(Entity<Guid>.Version))` against the `AggregateVersionConvention`-adjacent `InvalidOperationException`; the rewritten message ("...'{0}' implements IAggregateRoot but has no mapped 'long Version' property. Aggregate roots must inherit Entity<TId> so Version is mapped by convention...") still contains both literal substrings, so the assertions survive unchanged. Separately confirmed `domain-invariants-guard-tests.cs` (the test file that exercises `MissingInvariantsValidatorException`) asserts nothing about message text at all, so the other two rewritten strings carry zero test-coupling risk.
- TWA0011/TWA0012 accuracy re-verified directly against `aggregate-invariants-analyzer.cs`: TWA0011 fires when no nested `Invariants : AbstractValidator<T>` exists; TWA0012 fires per non-private qualifying validator. `AggregateDbContext.SaveChanges`/`SaveChangesAsync` do call `DomainInvariantsGuard.EnsureValid(...)` before `base.SaveChanges(Async)` — the skill's and message's claims about the hook location are accurate.
- TWA0004 exclusion for skills confirmed structurally: the analyzer (`purpose-region-analyzer.cs`, TWA0004) only fires on C# syntax trees during Roslyn analysis; `.md` skill files are never compiled, so they're excluded by construction, not by a special-case rule.

## Issues

### Nit — Exemplar section drops one factual detail from the source doc

- **Severity:** nit
- **File:** `skills/tw-aggregate-pattern/SKILL.md` (Exemplar section, ~line 70-76)
- **Description:** The retired doc's closing paragraph said the profile EF mapping covers
  "schema `profiles`, TypedId conversion." The skill's Exemplar section keeps the file pointer and
  the `PostgresDbContext`/`ApplyConfigurationsFromAssembly`/`Version` convention detail, but drops
  the "schema `profiles`, TypedId conversion" characterization of what that mapping file actually
  configures. It's the one specific factual detail from the original 37 lines that didn't make it
  across in some form (everything else was either carried verbatim or reorganized into the new
  Enforcement map / Related-skills sections without loss). Confirmed against the real file
  (`profile-entity-type-configuration-infrastructure.cs`) that both the schema-per-slice
  (`ToTable(TableName, SchemaName)`, both "profiles") and the TypedId `.HasConversion` mapping are
  real and still true — so the dropped sentence isn't stale, just omitted.
- **Suggestion:** Optional — could reinstate "(table/schema `profiles`, TypedId key conversion)"
  next to the file pointer for full parity, but this is genuinely minor content a reader would
  find by opening the exemplar file anyway, and the checklist's "content parity" bar is clearly
  met on every substantive pattern rule. Not blocking.
- **Status:** open

No other issues found. Deletion, referrer sweep, message self-direction, XML examples, region
reconciliation, and test-assertion survival all check out against the repo as it stands.
