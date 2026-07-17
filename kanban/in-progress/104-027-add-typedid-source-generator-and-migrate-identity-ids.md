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

- [ ] `[TypedId]` attribute injection + incremental generator (BCL surface)
- [ ] Generated STJ JsonConverter (string form, fail-closed read, dictionary keys)
- [ ] Parsing/TypeConverter/IComparable members
- [ ] EF ValueConverter + ConfigureConventions generation into EF-referencing compilations
- [ ] Migrate PrincipalId + CredentialId; delete hand-written duplicated members
- [ ] Generator snapshot tests
- [ ] Identity round-trip/parse/compare tests
- [ ] `dev build` 0/0; all identity + analyzer test projects pass

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
`RegisterPostInitializationOutput` emits public sealed `TimeWarp.Architecture.TypedIdAttribute`
(AttributeTargets.Struct). Public so EF-host compilations can see it on referenced id types.
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
