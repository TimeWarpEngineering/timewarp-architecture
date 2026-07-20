# Add TypedId source generator and migrate identity ids

## Parent

104

## Description

Add a `[TypedId]` incremental source generator to `TimeWarp.Architecture.Generators` that emits the
house typed-id shape onto `readonly partial record struct` declarations, then migrate `PrincipalId`
and `CredentialId` in `TimeWarp.Identity` to it. **Gates 104-003**: the generated `JsonConverter`
closes a proven fail-open serialization bug (see Notes) that would otherwise ship in the first
identity contract.

Declaration collapses to:

```csharp
[TypedId]
public readonly partial record struct PrincipalId;
```

## Requirements

- Generated in the id's own assembly (BCL-only, keeps `TimeWarp.Identity` dependency-free):
  - `Value` (Guid), `IsEmpty`, private ctor
  - `New()` → `Guid.CreateVersion7()`; `From(Guid)` rejects `Guid.Empty`
  - `IComparable<T>`; `IParsable<T>`/`ISpanParsable<T>`/`TryParse`; `TypeConverter`
  - `System.Text.Json` `JsonConverter` applied via `[JsonConverter]`: writes the **plain Guid
    string** (not a nested object), **throws on empty at read** (fail closed, mirroring `From`);
    include dictionary-key read/write support
  - `ToString()` → Guid string
- EF Core support generated into **consuming** assemblies, not the id's assembly: when a
  compilation references both EF Core and `[TypedId]` types, emit `ValueConverter` classes there,
  plus one generated `ConfigureConventions` extension registering all of them (no per-id
  hand-registration)
- `[TypedId]` attribute injected by the generator itself (`RegisterPostInitializationOutput`,
  internal) — NOT placed in `TimeWarp.Architecture.Attributes`; it is compile-time-only metadata
  and must not add a runtime dependency to `TimeWarp.Identity`
- Migrate `principal-id.cs` and `credential-id.cs` to `[TypedId]` declarations; Purpose/Design
  regions stay in the hand-written file; existing identity tests stay green
- Tests: generator snapshot tests in the analyzers test project; in `timewarp-identity-tests`:
  JSON round-trip (string form, empty-rejected-on-read), `Parse`/`TryParse`, `TypeConverter`,
  comparison ordering
- No Dapper, no Newtonsoft, no Swagger schema output — out of scope until a consumer exists
  (OpenAPI rendering, if ever wrong, is a one-line host schema mapping)

## Checklist

- [x] `[TypedId]` attribute injection + incremental generator (BCL surface)
- [x] Generated STJ JsonConverter (string form, fail-closed read, dictionary keys)
- [x] Parsing/TypeConverter/IComparable members
- [x] EF ValueConverter + ConfigureConventions generation into EF-referencing compilations
- [x] Migrate PrincipalId + CredentialId; delete hand-written duplicated members
- [x] Generator snapshot tests
- [x] Identity round-trip/parse/compare tests
- [x] `dev build` 0/0; all identity + analyzer test projects pass

## Notes

### Why a generator (design rationale, 2026-07-17)

Evaluated adopting [andrewlock/StronglyTypedId](https://github.com/andrewlock/StronglyTypedId)
instead. Rejected, but its generated-member list is the spec baseline for ours. Summary:

- **Project health disqualifying for a trust kernel**: latest release 1.0.0-beta08 (2024-04-25),
  prerelease for 3+ years with two breaking mid-beta redesigns; last commit 2025-02; 27 open
  issues. Not archived, but stalled.
- **Its defaults conflict with our RFC-mandated invariants** — public ctor (private-ctor option
  rejected, issue #75), `Guid.NewGuid()` with no v7 support anywhere in the codebase, ships
  `static Empty` as a *valid* value, no empty-rejection (issue #84: "write a custom template").
  Adopting it means writing custom templates that re-implement our own invariants — the library
  would contribute only the copy-paste mechanism.
- **No `record struct` support** (issue #46, open since 2021) — we would downgrade to plain
  `struct` with generator-managed equality; our generator keeps compiler-generated record equality
  and emits only the members around it.
- **No templates needed**: StronglyTypedId has a template DSL because it serves everyone's
  opinions. A house generator hard-codes exactly one opinion. Future variation = attribute
  property, never a template file.

Why now, honestly costed: a production incremental generator + snapshot tests is more work than
hand-writing 2×~80 lines — if two ids were the end of it, hand-written wins. But EF is confirmed
(not YAGNI), 104-004+ will mint more ids (session/token; x402 payment/challenge), the repo
standing directive is *prefer sourcegen over agreement-by-memory* (two hand-mirrored types is
exactly that), and right now there are two ids and zero consumers outside the library — the
cheapest migration moment this will ever have. The generator ships in the already-published
`TimeWarp.Architecture.Generators` package, so greenfield template apps get `[TypedId]` for free.

### Why the EF split (load-bearing decision)

The 104-002 RFC keeps `TimeWarp.Identity` dependency-free. A nested `ValueConverter` inside the id
type would force an EF Core reference onto the identity library — the StronglyTypedId trap in
different clothes. Therefore two generator outputs: BCL surface into the id's assembly; EF
artifacts into whatever assembly references both EF and the id types (server/infrastructure). The
generated `ConfigureConventions` extension exists so converter registration is not itself
agreement-by-memory.

### The bug this gates on (proven 2026-07-17)

STJ round-trip probe of current `PrincipalId`: serializes as
`{"Value":"019f…","IsEmpty":false}` and deserializes back as the **empty id with no exception**
(private ctor + get-only property → STJ silently returns `default`). The first contract in 104-003
carrying an id would silently receive empty principals. Fail-open — same class of defect the RFC's
Decision 2 eliminated for enums.

### Known limitation (accepted)

`default(TypedId)` is unguardable — structs always have an implicit parameterless ctor. `IsEmpty`
checks at use sites remain the mitigation (same caveat applies to every typed-id approach,
including StronglyTypedId, per its issue #84 thread).

### Depends on

104-002 (done). **Blocks 104-003** — do this first so identity contracts never carry the raw
serialization gap.

## Session

- Created: 2026-07-17 (architecture review follow-up; StronglyTypedId comparison)

### Implementation plan (104-027)

#### Generator home
`source/analyzers/timewarp-architecture-analyzers/generators/typed-id-source-generator.cs`
Package: TimeWarp.Architecture.Generators (existing).

#### Attribute injection
`RegisterPostInitializationOutput` emits `TimeWarp.Architecture.TypedIdAttribute`
(AttributeTargets.Struct). ~~Public so EF-host compilations can see it on referenced id types.~~
**Superseded by Review fold-in finding 1: internal** — public caused CS0436, and Roslyn reads
attribute applications from metadata regardless of attribute-class accessibility.
Still no Attributes package / no Identity package reference — attribute source is generated into each
compilation that runs the generator.

#### BCL surface (per [TypedId] partial readonly record struct in this compilation)
Emit `{Type}.TypedId.g.cs`:
- private ctor(Guid value)
- Value, IsEmpty
- New() CreateVersion7, From rejects Empty
- IComparable<T>, CompareTo
- IParsable/ISpanParsable TryParse/Parse
- TypeConverter nested or companion
- JsonConverter: plain Guid string write; read throws on empty; dictionary key support
- [JsonConverter(typeof(...))] on the partial
- ToString → Guid string

#### EF surface (only if compilation references EF Core ValueConverter)
Scan TypedId types declared **in this compilation and referenced assemblies** (attribute metadata name).
Emit `{Type}ValueConverter.g.cs` + single `TypedIdModelBuilderExtensions.g.cs` with
`ConfigureTypedIdConventions(this ModelConfigurationBuilder)` registering all converters.
Do **not** emit EF into TimeWarp.Identity (no EF ref).

#### Wire Identity
- ProjectReference Generators as Analyzer on `timewarp-identity.csproj`
- Collapse principal-id.cs / credential-id.cs to partial + Purpose/Design + [TypedId]

#### Tests
- `typed-id-source-generator-tests.cs` in sourcegenerator-tests (snapshot/contain assertions)
- Identity tests: JSON string form, empty reject on read, Parse/TryParse, TypeConverter, Compare

#### Verify
dev build 0/0; fixie identity tests + sourcegenerator tests

## Session
- Started: 2026-07-17 (orchestrate-task 104-027)

## Results

### Summary
Added `[TypedId]` incremental source generator to `TimeWarp.Architecture.Generators` and migrated
`PrincipalId` / `CredentialId` to partial declarations. Generated STJ converter serializes as plain
Guid string and **throws on empty** at read (closes the proven fail-open STJ default bug). EF
ValueConverters + `ConfigureTypedIdConventions` emit only when EF is referenced (not on Identity).

### Files changed
| Action | Path |
|--------|------|
| Created | `source/analyzers/.../generators/typed-id-source-generator.cs` |
| Edited | `timewarp-architecture-analyzers.csproj` (package description) |
| Edited | `timewarp-identity.csproj` (generator Analyzer dual-mode) |
| Edited | `principal-id.cs`, `credential-id.cs` → `[TypedId]` partials |
| Created | `typed-id-source-generator-tests.cs` |
| Edited | `principal-id-tests.cs`, `credential-id-tests.cs` |

### Key decisions
- Attribute injected in `TimeWarp.Architecture` (not Attributes package); **internal** per Review fold-in finding 1 (public caused CS0436; metadata scanning never needed public)
- Comparison operators emitted for CA1036 with IComparable
- EF converters internal in `TimeWarp.Architecture.TypedIds.Ef`
- Identity remains EF-free
- Known: dual attribute emission CS0436 if host both refs Identity and re-emits attribute — follow-up when first EF host attaches generator

### Build / tests
- `dev build`: 0 warnings / 0 errors
- `dotnet fixie tests/libraries/timewarp-identity-tests`: **71 passed**
- `dotnet fixie tests/analyzers/timewarp-architecture-sourcegenerator-tests`: **26 passed**

### Gates
Unblocks **104-003** (WebAuthn / contracts can carry typed ids safely).

## Session
- Created: 2026-07-17
- Implementation: 2026-07-17 (orchestrate-task 104-027)
- Review: orchestrator spot-check on converter/empty/EF split; tests green
- Architecture review + fold-in: 2026-07-17 (see Review fold-in below); pending independent re-review

## Review fold-in (2026-07-17)

Post-completion architecture review found one confirmed defect and three debt items; per the
no-tech-debt rule all were fixed in the same task.

### Findings → fixes

1. **CS0436 build break (confirmed by probe)** — the injected `TypedIdAttribute` was `public`, so
   `timewarp-identity.dll` exported it; any second generator-running assembly referencing identity
   (104-004's session/token ids) got `warning CS0436` at every `[TypedId]` site → error under
   warnings-as-errors. The stated reason for `public` (EF metadata scanning) was wrong: Roslyn reads
   attribute *applications* from metadata regardless of attribute-class accessibility.
   **Fix**: both injected attributes are now `internal`. Probe re-run: 0 warnings. New test
   `Should_Not_Collide_When_Host_Declares_Own_Ids_And_References_Id_Assembly` locks it in.
2. **EF pass was non-incremental and walked every referenced assembly's namespace tree** (raw
   `CompilationProvider` → `RegisterSourceOutput`, ~200 framework assemblies per run in an IDE).
   **Fix**: compilations declaring ids are stamped with `[assembly: TypedIdsEmbedded]`; the EF pass
   only walks marked assemblies, and its output is keyed on an equatable model (HasEf + sorted id
   list) so it regenerates only when the id set or EF-ness changes.
3. **Latent: referenced-assembly ids were never discoverable** — `IsRecord` is false for record
   structs loaded from metadata (they compile to plain structs), so the old scan silently rejected
   every cross-assembly id; the old EF test only used same-compilation stubs and never caught it.
   **Fix**: metadata candidates are validated by generated member shape (`Value` Guid property +
   static `From`) instead of `IsRecord`. New cross-assembly test compiles an id assembly, references
   it from an EF host, and asserts the converter is emitted. (Transitive references are covered
   because MSBuild passes the transitive closure to the compiler — the review's initial "direct
   references only" concern was unfounded and is documented in the Design region.)
4. **Silent skip of invalid shapes** — `[TypedId]` on a non-partial/non-readonly/plain struct
   generated nothing; a contract carrying such an id would compile and serialize fail-open as `{}`.
   **Fix**: **TWE006** (Error) reports at the declaration; tests for non-partial record struct and
   plain struct. Also: multiple attributed partial declarations of one type now emit once instead of
   colliding on hint name.

### Verification (fold-in)
- `dev build`: 0 warnings / 0 errors
- `dotnet fixie tests/analyzers/timewarp-architecture-sourcegenerator-tests`: 32 passed
- `dotnet fixie tests/libraries/timewarp-identity-tests`: 71 passed
- CS0436 probe (second assembly with own attribute copy + identity reference): 0 warnings

### Still-open (accepted, on the publish checklist — not debt in this repo)
Package-mode (`UseAnalyzerPackages=true`) consumers of identity **source** need a published
`TimeWarp.Architecture.Generators` containing TypedId; ship Generators before or with the first
release that includes `TimeWarp.Identity` source. In-repo dual-mode is unaffected.
