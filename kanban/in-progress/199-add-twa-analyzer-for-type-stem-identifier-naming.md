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

- [ ] Confirm diagnostic id (TWA0023) and `isEnabledByDefault: false` (or Info)
- [ ] Analyzer + exception attribute (reason required)
- [ ] AnalyzerReleases.Unshipped.md row
- [ ] Tests: match, mismatch, interface strip, two-instance qualifier, primitive skip, opt-out
- [ ] AGENTS.md TWA table pointer to `tw-csharp` for the prose rule
- [ ] Do **not** turn the diagnostic to warning/error in this repo's Directory.Build / editorconfig
      as part of this task
- [ ] `dev build` 0/0; analyzer tests pass

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

## Session

- Created: 2996566 (2026-08-20)
- Briefed from flow Grok session after merging timewarp-flow PR 111
