# Harden Enumeration base class and consider extraction to own library

## Description

`source/foundation/foundation-domain/enumeration/enumeration.cs` (Bogard-pattern enumeration
class) is serviceable but has several gaps worth closing before usage grows. Separately, evaluate
extracting it into its own first-party library (`timewarp-enumeration` or similar) so it can be
consumed outside the foundation packages.

## Requirements

Improvements identified in review (2026-07-19):

- **Cache `GetAll<T>` reflection** — currently every `Parse`/`FromValue`/`FromName` call
  re-enumerates fields via reflection. Add a static `ConcurrentDictionary<Type, T[]>` (or
  equivalent) cache.
- **Implement `IEquatable<Enumeration>` and `==`/`!=` operators** — today equality goes through
  the boxing `Equals(object)` path, and `member1 == member2` is reference equality even though
  semantic equality is Value + exact type. Real foot-gun.
- **Add generic `IComparable<Enumeration>`** — only non-generic `IComparable` exists.
- **`FromString` collision safety** — name and alternate-code lookup share one `FirstOrDefault`;
  if one member's name equals another member's alternate code the result is silently
  order-dependent. Detect/throw on ambiguity, or define precedence explicitly (name wins).
- **System.Text.Json converter** — no STJ support today; a subclass in a contract would serialize
  as `{"Value":..,"Name":..,"AlternateCodes":[..]}` and be unreconstructible on read (no public
  ctor, get-only props) — same silent-failure class as the PrincipalId bug fixed in 104-027.
  Converter should round-trip by Value (or Name) and fail closed on unknown values.
- **Analyzer for member declaration shape** — `GetAll` only sees `public static readonly` fields
  (`DeclaredOnly`); a member declared as a property or non-public silently vanishes from lookups.
  This is agreement-by-memory — per the standing prefer-analyzers directive, add a TWA diagnostic
  flagging Enumeration-subclass members not declared in the required shape.

## Checklist

- [x] Cache GetAll reflection results
- [x] IEquatable + operator ==/!= (Value + exact type semantics)
- [x] IComparable\<Enumeration\>
- [x] FromString ambiguity handling
- [x] STJ JsonConverter (fail closed on unknown values) + round-trip tests
- [x] Analyzer: enumeration members must be public static readonly fields
- [x] Reconcile `#region Design` in enumeration.cs with the changes
- [x] Decide: extract to standalone library (`timewarp-enumeration` or similar)? — **decided 2026-10-04: keep in TimeWarp.Foundation.Domain until the trigger (see Decision note)**; analysis below under *Extract vs keep*

## Notes

- **See https://github.com/ardalis/SmartEnum for ideas.** Its feature surface (value converters,
  EF support, `TryFromName`/`TryFromValue`, comparison operators) is a useful checklist, same way
  StronglyTypedId was used as a spec for 104-027 rather than adopted.
- **Consider extracting to its own library** — `timewarp-enumeration` or similar — rather than
  keeping it inside `TimeWarp.Foundation.*`. A standalone package would let dependency-free
  libraries (e.g. timewarp-identity, should a behavior-carrying enumeration ever be needed there)
  consume it without dragging in the foundation stack.
- Context: review concluded the plain C# enums in `source/libraries/timewarp-identity`
  (TrustTier, PrincipalKind, CredentialType) should stay plain enums — they are pure
  discriminators and the identity library is deliberately runtime-dependency-free. This task is
  about the Enumeration class itself, not migrating identity.

### Cockpit note (2026-10-04)

- **Extraction is Steve's decision.** Implement the hardening items in place, under
  `source/foundation/foundation-domain/enumeration/`. For the last checklist item, write a short
  extract-vs-keep analysis in Notes:
  - who would consume it outside foundation;
  - the package and repo cost;
  - versioning against `TimeWarp.Foundation.*`;
  - what SmartEnum already covers.

  Then leave the decision as an open question for Steve. Do **not** create a new repo or package.
- The `==`/`!=` and `IEquatable` change alters semantics, from reference to Value plus exact
  type. Find every `==` on an Enumeration across the repo (`dotnet` search or Roslyn) and confirm
  that none relied on reference equality. Record the result.
- **Analyzer:** use the next free TWA id. Register it in the descriptor SSOT,
  `AnalyzerReleases.Unshipped.md`, the AGENTS.md enforcement table, and the Analyzers package row.
  An analyzer registry change means a full rebuild.
- **JSON converter:** contract serialization goes through `ContractSerializationDefaults`. Wire the
  converter so contract round-trips work, add round-trip tests (co-located Jaribu or
  `web-contracts-tests`), and fail closed on unknown values.
- `TimeWarp.Foundation.*` is a published package: public surface needs real XML docs, and
  `dev check-version` applies. Bump the version and pins in the same commit if required.
- The identity library now ships as the `TimeWarp.Identity` package, so the Notes path
  `source/libraries/timewarp-identity` may be stale. This task does not touch identity.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`. Do not start an
  AppHost.

### `==` reference-equality audit (2026-10-04)

`==`/`!=` on an Enumeration now means Value + exact runtime type, not reference identity. Searched
all of `source/` and `tests/` for Enumeration subclasses and their uses. The only production subclass
is `CorsPolicy` (foundation-server). Its call sites (`web-server`, `api-server` and `grpc-server`
`program.cs`, plus `AnyPolicy`/`ExamplePolicy`) use only `.Apply(...)` and `.Name`. There is no `==`, `!=`,
`Equals`, `ReferenceEquals`, or collection-membership comparison on any Enumeration. The test suite
(`foundation-domain-tests/enumeration-tests.cs`) had none either. **No code relied on reference
equality**, so the semantic change breaks nothing.

### Extract vs keep (analysis for Steve — decision left open)

- **Who would use it outside foundation:** nobody today. In this repo the only subclass is
  `CorsPolicy` (foundation-server). `TimeWarp.Identity` deliberately keeps plain enums (TrustTier,
  PrincipalKind, CredentialType are pure discriminators and the library stays dependency-free), and
  this task did not change that. Likely future users would be dependency-free libraries
  (identity, x402, Nuru-based CLIs) that want behavior-carrying members without taking
  `TimeWarp.Foundation.*`. So the case is speculative: no consumer is waiting on it.
- **Package and repo cost:** a new repo (or a new package in this monorepo) means a csproj, CPM pin,
  release workflow, trusted-publishing setup, README/skill, and its own `dev check-version` line.
  `TimeWarp.Foundation.Domain` is already tiny and dependency-free (Entity, Enumeration,
  IAggregateRoot), so extraction would mostly rename a package that is already light. A middle path
  is a new `TimeWarp.Enumeration` package built from this monorepo's `source/libraries/`
  (the same model as `TimeWarp.Modules`). That avoids a new repo and release pipeline.
- **Versioning against `TimeWarp.Foundation.*`:** today Enumeration ships in lockstep with the
  foundation version (task-124 policy: one version, pins equal `<Version>`). If extracted to its own
  repo, it gets its own version line. Foundation then pins it like any external dependency, and a fix
  needs two releases. If it stays a monorepo package, lockstep versioning stays and costs nothing.
- **What SmartEnum already covers:** Ardalis.SmartEnum covers most of this:
  - `FromValue`/`FromName` and their `TryFrom*` forms
  - equality and comparison operators
  - STJ, Newtonsoft, EF Core, Dapper, AutoFixture, MessagePack, ProtoBuf, and Utf8Json adapters
  - generic `TValue`
  - `SmartFlagEnum`

  Things SmartEnum lacks that this class has:
  - `AlternateCodes` / `FromString` (external-system code mapping)
  - fail-closed ambiguity validation
  - the TWA0028 member-shape analyzer (SmartEnum still discovers by reflection, with the same
    silent-miss risk)

  Adopting SmartEnum instead would trade AlternateCodes and the analyzer for a third-party dependency.
- **Coupling introduced by this task:** to wire the converter into `ContractSerializationDefaults`,
  `foundation-contracts` now references `foundation-domain`. Domain is dependency-free, so this costs
  little, but contracts now depend on a domain package. A standalone enumeration package would turn
  that edge into contracts → enumeration only. This is the strongest concrete argument for
  extraction.
- **Decided (2026-10-04, see Decision note):** was an open question — keep Enumeration in `TimeWarp.Foundation.Domain` (status quo after
  this task), split it into a monorepo `TimeWarp.Enumeration` package, or move it to its own repo?
  Recommendation: keep it until a non-foundation consumer exists. If the contracts → domain edge is
  unwanted, choose the monorepo-package middle path, not a new repo.

## Results

Hardened `Enumeration` in place (`source/foundation/foundation-domain/enumeration/`):

- **Cache:** each subclass gets a `MemberCache<T>` (generic static holder) with a read-only member list and
  Value/Name/alternate-code/string indexes. Every `From*`/`TryFrom*` is a dictionary lookup. A
  snapshot taken while the subclass's static initializer is still running is not cached.
- **Equality:** `IEquatable<Enumeration>`, `==`/`!=` (Value + exact runtime type; null-safe).
- **Comparison:** `IComparable<Enumeration>` plus `<`, `<=`, `>`, `>=` (by Value; null sorts first). The
  foundation `CA1036` NoWarn is removed because the full operator set now exists.
- **Ambiguity fails closed:** two members sharing a Value, a Name, or an alternate code, or one
  member's Name equalling another's alternate code, throw `InvalidOperationException` on first
  lookup. `FromString` no longer depends on declaration order. A member's name may still equal its
  own alternate code.
- **New non-throwing lookups:** `TryFromValue`, `TryFromName`, `TryFromString` (from SmartEnum's API).
- **JSON:** `EnumerationJsonConverter<T>` and `EnumerationJsonConverterFactory` write the member
  `Name`. Reads are ordinal and fail closed (`JsonException`) on an unknown name, a number, an
  object, or any other non-string token. JSON null round-trips. Dictionary keys are supported.
  `ContractSerializationDefaults.Apply` registers the factory, which adds the
  `foundation-contracts` → `foundation-domain` ProjectReference.
- **Analyzer TWA0028** (`EnumerationMemberShapeAnalyzer`): a static field or property typed as the
  Enumeration subclass (or a subtype) must be `public static readonly`. Static properties,
  non-public fields, and mutable fields are flagged. It is registered in
  `AnalyzerReleases.Unshipped.md`, the AGENTS.md enforcement table and Analyzers package row, the
  convention-analyzers csproj description, and the root `source/Directory.Build.props` comment.
- Design regions are reconciled in `enumeration.cs` and `contract-serialization-defaults.cs`, and
  new files carry Purpose and Design regions.
- **Version:** `dev check-version` reports source 2.0.0-beta.20 is newer than the published
  2.0.0-beta.19, so no bump is needed.

Gates (2026-10-04, this worktree):

- `dev build --clean`: 0 warnings / 0 errors
- `dev test`: 21/21 suites passed (foundation-domain-tests 62, foundation-contracts-tests 25, analyzers suite including 7 new TWA0028 cases)
- `dev template-smoke`: SUCCEEDED
- `ganda repo audit`: passes all checks
- No AppHost was started.

### How to validate

**Smoke:**

```bash
cd tests/foundation/foundation-domain-tests && dotnet test -c Release
cd ../foundation-contracts-tests && dotnet test -c Release -- --filter-class Enumeration_Given_
cd ../../analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release -- --filter-class Enumeration
```

**Expect:**

- foundation-domain-tests: 62/62 pass, including `Equality_Operators`, `Comparison_Operators`,
  `Ambiguity`, `TryFrom`, and `Json_Round_Trip`.
- foundation-contracts-tests `Enumeration_Given_`: 4/4 pass. A contract DTO serializes to
  `{"title":"Outage","priority":"High","escalation":null}` through
  `ContractSerializationDefaults.Options`, and `"Urgent"` and `2` both throw `JsonException`.
- analyzers `Enumeration`: 7/7 pass (static property, internal field, mutable field, and indirect
  subclass flag TWA0028; correct shape, a non-Enumeration type, and a same-named foreign base are clean).
- Manual check: in `CorsPolicy`, change `public static readonly CorsPolicy Any` to
  `public static CorsPolicy Any { get; } = …`. `dev build` should fail with TWA0028.

### Review disposition

- **Rounds:** 1, effort 3 (by-diff budget). Roster: `general`.
- **Final counts:**
  - bug: 0
  - suggestion: 0
  - nit: 2, both wontfix
  - open: 0
- **Disposition:** `accepted-exceptions`
  - M1: a permanently-null member field defeats the cache. It costs performance only; results stay correct.
  - M2: cross-subclass CompareTo vs Equals. Value-only comparison is documented and predates this change.
- **Gates re-verified by the reviewer:**
  - `dev build`: 0/0
  - foundation-domain-tests: 62/62
  - foundation-contracts-tests: 25/25
  - analyzers: 192/192
  - `ganda repo audit`: pass
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`

### Decision (2026-10-04, Steve)

Option 2 chosen: no contracts -> domain reference; `EnumerationJsonConverterFactory` is unregistered
by default (the converter, factory, tests and TWA0028 stay in foundation-domain; apps opt in on their
own `JsonSerializerOptions`). Extraction is deferred with this trigger: the first contract that
genuinely needs an Enumeration (per-member behavior a plain enum cannot carry) triggers extracting
Enumeration + its converter into a small standalone package that both contracts and domain
reference, then registering the converter in `ContractSerializationDefaults`. Until then it stays in
`TimeWarp.Foundation.Domain`. The extraction open question is closed.

## Session

- Created: 2026-07-19
- 2026-10-04 (implementer, ganda task work): implemented all hardening items. Extraction left as an
  open question with analysis. Gates green (build 0/0, test, template-smoke, audit).
- 2026-10-04 (review oracle, ganda task work): tw-implementation-review round 1 (general, effort 3) → accepted-exceptions (0 open; 2 nits wontfix). Gates re-verified.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-04T15:57:36Z
