# Add TWA analyzer for type-stem identifier naming

## Description

Enforce the type-stem identifier rule from flow skill `tw-csharp`: for a named type that already
names the role, the identifier **is the type name**. Two of the same type qualify the stem and keep
the type as the **head** (`CatalogHttpClient`, not `CatalogClient`). Interfaces drop leading `I`.
Do not invent a second vocabulary (`Discovery` for `OriginHomeDiscovery`, `Depends` for
`KanbanDependService`).

Origin: timewarp-flow task **111** / [PR 111](https://github.com/TimeWarpEngineering/timewarp-flow/pull/111)
(merged). Rule text SSOT stays in flow `skills/tw-csharp/SKILL.md`; this task is the analyzer.

## Requirements

- New convention analyzer in `source/analyzers/timewarp-architecture-convention-analyzers/`
  (package `TimeWarp.Architecture.Analyzers`, TWA*). Proposed id **TWA0023** (confirm unused;
  TWA0005 is retired, TWA0017–0019 are generator/ingress).
- **Default off or Info.** Agent-written code across repos will have many violations. Do not enable
  as Warning/Error repo-wide until a cleanup wave.
- **Exception hatch required:** attribute with a non-empty `reason` (precedent:
  `[CrossSliceReference(typeof(T), reason)]` on TWA0009) and/or `dotnet_diagnostic.TWA0023.*`
  editorconfig exclusions.
- Skip primitives and untyped boxes (`string`, `int`, `bool`, `List<T>`, `Dictionary<,>`) by
  design. Name the meaning (`Title`, `Count`, `PendingTaskIds`) — not Hungarian (`titleString`).
- Strip leading `I` from interface type names (`IFileSystem` → `FileSystem`; two instances →
  `LocalFileSystem`).
- Qualified names keep the type as the head (`ReceivingPerson`, `CatalogHttpClient`).
- Rare documented exception: a type renamed for a global collision (`TimeWarpTerminal`) may drop
  the vendor prefix on the member (`Terminal`). Do not clip domain words.
- Tests in `tests/analyzers/` (positive, negative, interface `I`-strip, qualifier-head, primitive
  skip, exception attribute).
- Docs: Architecture `AGENTS.md` TWA table. Do not duplicate the rule from `tw-csharp`.

Not SourceGenerators (TW0001) unless this task discovers the rule must ship to repos that do not
take Architecture analyzers. Not Ganda. Not flow.

## Checklist

- [x] Confirm diagnostic id (TWA0023) and `isEnabledByDefault: false` (or Info)
- [x] Analyzer + exception attribute (reason required)
- [x] AnalyzerReleases.Unshipped.md row
- [x] Tests: match, mismatch, interface strip, two-instance qualifier, primitive skip, opt-out
- [x] AGENTS.md TWA table pointer to `tw-csharp` for the prose rule
- [x] Do **not** turn the diagnostic to warning/error in this repo's Directory.Build / editorconfig
      as part of this task
- [x] `dev build` 0/0; analyzer tests pass

## Notes

`.editorconfig` `dotnet_naming_rule.*` can enforce PascalCase vs camelCase only. Stem matching
needs a Roslyn analyzer.

Convention analyzers are wired repo-wide via `source/Directory.Build.props` and are safe to
reference from every project (no generators). Default-off still matters: first enable will light
up existing agent-written names (`Log`, `Cts`, `Plugin`, `Discovery`, `Depends`).

### Rule (copy of flow tw-csharp — keep in sync if the skill moves)

One instance — exact type name:

```csharp
private readonly OriginHomeDiscovery OriginHomeDiscovery;
private readonly IFileSystem FileSystem;
```

Two instances — qualify the stem; type is the head:

```csharp
HttpClient CatalogHttpClient;
HttpClient BillingHttpClient;
```

```csharp
// ✗
OriginHomeDiscovery Discovery;
KanbanDependService Depends;
HttpClient CatalogClient;
```

Casing is already editorconfig: class-scope PascalCase, method-scope camelCase, no underscore.
This analyzer is stem identity, not casing.

### Implementation plan (Phase 2)

TWA0023 is unused (only the kanban brief). Next free convention ID. Convention analyzers declare
their own `DiagnosticDescriptor` (not `diagnostics/diagnostic-descriptors.cs`). Already wired
repo-wide; default-off is what keeps the template silent.

**Diagnostic**

| Knob | Value |
|------|--------|
| ID | TWA0023 |
| Category | `Naming` |
| DefaultSeverity | Warning |
| `isEnabledByDefault` | `false` |
| This repo editorconfig / Directory.Build | do **not** add `dotnet_diagnostic.TWA0023.severity` |
| Consumer opt-in | `dotnet_diagnostic.TWA0023.severity = warning` |
| Package | `TimeWarp.Architecture.Analyzers` |

Info-by-default still nags IDEs. Default-off matches “do not enable until a cleanup wave.”

RS2008: Unshipped.md row; Severity cell likely `Disabled` when default-off. Bump package-range
comments `TWA0002–0016, TWA0020–0022` → `TWA0002–0016, TWA0020–0023` (AGENTS.md package table,
`source/Directory.Build.props`, convention-analyzers csproj Description). Do not fold TWA0017–0019
(Generators) into that range.

**Match algorithm**

1. Declared type; unwrap `Nullable<T>` / nullable annotations.
2. Skip set → return.
3. Stem = `type.OriginalDefinition.Name` (no arity: `ILogger<T>` → `ILogger`).
4. Interface + `I` + uppercase → strip `I`. Empty after strip → skip.
5. Identifier **ends with** the stem (`OrdinalIgnoreCase`). Exact match is equal-length suffix.
   `CatalogHttpClient` ✓; `CatalogClient` ✗; `Discovery` ✗ vs `OriginHomeDiscovery`.
6. Casing is not this rule (`HttpClient` field / `httpClient` local both pass).

**Do not auto-detect vendor-prefix clipping.** `TimeWarpTerminal` → `Terminal` is attribute-only.

**In:** fields, properties (not indexers), parameters (methods/ctors/primary ctors/lambdas),
locals, `out var`, deconstruction, `is` designations, foreach, catch. Locals via syntax
(`VariableDeclaratorSyntax` / `SingleVariableDesignationSyntax` / `ForEachStatementSyntax` /
`CatchDeclarationSyntax`) — `RegisterSymbolAction` does not support `SymbolKind.Local`.

**Out:** method/type names, events, extension `this`, setter `value`, discards, implicit/compiler-
generated, overrides + explicit interface members and their parameters (name not free), indexers,
anonymous-type members, `TypeKind.Error`, type parameters, pointers, function pointers, `dynamic`.
Record positional params: analyze the parameter; skip the synthesized property.

Catch + lambda parameters **are in** (noise is why the rule ships off). Razor `@code` **out of v1**
(`GeneratedCodeAnalysisFlags.None`, like TWA0009/TWA0021, not TWA0022).

**Skip set**

- `SpecialType` primitives + `string`/`object` + arrays.
- Do **not** skip `DateTime`, `Guid`, `TimeSpan`, `CancellationToken`, enums, `ILogger<T>`,
  `IHttpClientFactory` (`factory` fails; `httpClientFactory` is the rule working).
- Untyped boxes by `OriginalDefinition` metadata name: `List`1`, `Dictionary`2`, `HashSet`1`,
  `Queue`1`, `Stack`1`, `IEnumerable`1`, `ICollection`1`, `IList`1`, `IReadOnlyList`1`,
  `IReadOnlyCollection`1`, `IDictionary`2`, `IReadOnlyDictionary`2`, `IQueryable`1`,
  Immutable/Concurrent variants, Span/Memory, `Task`/`Task`1`/`ValueTask`/`ValueTask`1`,
  `Action`/`Func` all arities, tuples, non-generic `IEnumerable`/`IList`/`IDictionary`.
- Hard-coded `ImmutableHashSet` in Design; not editorconfig-configurable in v1.

**Diagnostic**

```
title: "Identifier does not use the type stem"
messageFormat: "Identifier '{0}' must end with type stem '{1}' (the type name already names the role; qualify with a prefix if there are two of this type)"
```

Location: identifier token.

**Exception hatch** — `TimeWarp.Architecture.Attributes`, simple-name match (no ProjectReference
from convention-analyzers):

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter,
  AllowMultiple = false, Inherited = false)]
public sealed class TypeStemIdentifierAttribute : Attribute
{
  public string Reason { get; }
  public TypeStemIdentifierAttribute(string reason) => Reason = reason;
}
```

Empty/whitespace reason still flags TWA0023 (no second id). Locals/foreach have no
`AttributeTargets.Local` — hatch is `#pragma` / editorconfig. Do not wire Attributes repo-wide.

**Files to add**

- `source/analyzers/timewarp-architecture-convention-analyzers/type-stem-identifier-analyzer.cs`
- `source/analyzers/timewarp-architecture-attributes/type-stem-identifier-attribute.cs`
- `tests/analyzers/timewarp-architecture-analyzers-tests/type-stem-identifier-analyzer-tests.cs`

**Files to edit**

- `AnalyzerReleases.Unshipped.md` (TWA0023 row)
- convention-analyzers + attributes csproj Descriptions
- `source/Directory.Build.props` comment range only
- `AGENTS.md` TWA table row + package-table range

**Do not edit:** `.editorconfig`, `diagnostics/diagnostic-descriptors.cs`, flow `tw-csharp`,
Ganda, TW0001, product cleanup.

**Tests** (Microsoft.CodeAnalysis.Testing + Jaribu `RegisterTests<>`, copy TWA0022 harness).
Enable via globalconfig `dotnet_diagnostic.TWA0023.severity = warning`. Descriptor assertion
(`IsEnabledByDefault == false`) is the load-bearing default-off proof. Stub the attribute in
test source (TWA0009 pattern). Matrix: exact match, mismatch, I-strip, qualifier-head, primitives,
boxes, opt-out + empty reason, foreach, discard, arrays, override skip, pragma.

**AGENTS.md row** — pointer only, do not paste tw-csharp examples:

`TWA0023 | type-stem identifiers: named type that already names the role **is** the identifier
(strip leading I on interfaces; two of the same type qualify with the type as head). **Default
off** — enable with editorconfig. Opt-out: [TypeStemIdentifier(reason)]. Rule prose: flow skill
tw-csharp.`

**Out of scope:** enabling in this repo, code fix, vendor-prefix heuristic, configurable skip
lists, razor `@code`, shipping in TW* SourceGenerators, Foundation, second diagnostic for empty
reason.

**Order:** attribute → analyzer + Unshipped.md → tests → AGENTS.md → `dev build` 0/0 (must stay
0/0 **because the rule is off**).

**Resolved (not blockers):** default-off not Info; no vendor-prefix auto-detect; Attributes
package + simple-name match; catch/lambda in; razor out; Task/Func skipped, ILogger not; locals
pragma-only; overrides skipped.

## Session

- Created: 2996566 (2026-08-20)
- Briefed from flow Grok session after merging timewarp-flow PR 111
- Orchestrator: grok session (2026-08-20) — claimed, in-progress, plan finalized
- Phase 4b: effort 1 general review; round-1 M1 (do-not-skip test coverage) fixed; round-2 clean; disposition `clean`
- Implementer: TWA0023 analyzer + `[TypeStemIdentifier]` + tests; default-off

## Results

### What was implemented

TWA0023 type-stem identifier convention analyzer in `TimeWarp.Architecture.Analyzers`, **default off**.

- Identifier of a named type must **end with** the type stem (`OrdinalIgnoreCase`). Interfaces drop a leading `I` when `I` + uppercase. Qualifiers keep the type as the head (`CatalogHttpClient`).
- **In:** fields, properties (not indexers), parameters, locals, foreach, catch, lambdas.
- **Out:** methods/types/events, extension `this`, setter `value`, discards, implicit/compiler-generated, overrides + explicit interface members and their parameters, indexers, anonymous-type members, enum members, error/type-parameter/pointer/function-pointer/dynamic, record synthesized positional properties.
- Skip set: SpecialType primitives + arrays + untyped boxes (`List`/`Dictionary`/`IEnumerable`/`Task`/`Action`/`Func`/tuples/…). **Not** skipped: `DateTime`, `Guid`, `TimeSpan`, `CancellationToken`, enum *types*, `ILogger<T>`, `IHttpClientFactory`.
- Opt-out: `[TypeStemIdentifier(reason)]` in Architecture.Attributes, simple-name match, non-empty reason required. Empty/whitespace still flags. Locals/foreach hatch is `#pragma` / editorconfig.
- Consumer opt-in: `dotnet_diagnostic.TWA0023.severity = warning`. **Not** enabled in this repo’s `.editorconfig` or Directory.Build.

### Files changed

**Added**
- `source/analyzers/timewarp-architecture-convention-analyzers/type-stem-identifier-analyzer.cs`
- `source/analyzers/timewarp-architecture-attributes/type-stem-identifier-attribute.cs`
- `tests/analyzers/timewarp-architecture-analyzers-tests/type-stem-identifier-analyzer-tests.cs`

**Edited**
- `source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md` (TWA0023, Severity `Disabled`)
- convention-analyzers + attributes csproj Descriptions
- `source/Directory.Build.props` (comment range only)
- `AGENTS.md` (TWA table row + package-table range)

### Key decisions / deviations

- Default-off (`isEnabledByDefault: false`), not Info.
- No vendor-prefix auto-detect; `TimeWarpTerminal` → `Terminal` is attribute-only.
- Roslyn 5.6.0 has no `ILocalSymbol.IsDiscard` — skip identifier `_` and `IParameterSymbol.IsDiscard`.
- Enum members skipped (named values, not a role of the enum type).
- `var` on casts only (IDE0007 / TreatWarningsAsErrors; siblings do this).

### Test outcomes

- Analyzer tests **27/27 passed**: `cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release -- --filter-class Type_Stem`
- Descriptor: `TWA0023`, `IsEnabledByDefault == false`, `DefaultSeverity == Warning`
- `dev build` 0/0 (rule did not light up — default-off proof)

### Phase 4b review

- **Effort:** 1 (general only)
- **Rounds:** 2
- **Roster:** general
- **Paths:** `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/round-2/{general,merged}.md`, `review/disposition.md`
- **Final counts:** bug 0; suggestion 1 fixed (M1 do-not-skip true-positives); nit 0; open 0
- **Disposition:** `clean`

### How to validate

**Automated**

```bash
cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release -- --filter-class Type_Stem
# expect: 27 passed, 0 failed
```

If TWA0023 is missing from discovery, rebuild convention-analyzers:

```bash
dotnet build source/analyzers/timewarp-architecture-convention-analyzers --no-incremental -c Release
```

**Smoke (rule stays silent in this repo)**

```bash
dev build
# expect: 0 Warning(s) 0 Error(s) — TWA0023 must not appear
```

**Expect (opt-in elsewhere, not this repo)**

A consumer enables with:

```
dotnet_diagnostic.TWA0023.severity = warning
```

Then `HttpClient CatalogClient` warns TWA0023 (stem `HttpClient`); `HttpClient CatalogHttpClient` and `IFileSystem FileSystem` are clean. `[TypeStemIdentifier("reason")]` suppresses a mismatch; empty reason does not.

**Not in scope:** enabling TWA0023 in this template repo; product-code rename cleanup; a code fix.
