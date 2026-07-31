---
name: tw-feature-placement
description: >-
  **TIMEWARP SKILL** — feature-cohesive folder placement, per-use-case folders, and the
  filename grammar `<name>[-<function>]-<layer>.cs` for product code under `<family>/features/`
  and platform clusters under `<family>/platform/` (family-generic: web, api, grpc all share this
  machinery — worked examples below use web): which layer a file belongs to, what to name it
  and which folder it goes in, the registry that backs TWA0015/TWA0016, and the membership-guard
  build errors. Invoke before creating, moving, or renaming a file under a feature slice or
  platform cluster, or when a TWA0015/TWA0016/membership-guard error appears.
  WHEN: "Where does this handler file go?", "What do I name this contract file?",
  "Should this file get its own folder?", "TWA0015", "TWA0016",
  "feature file matches no registered layer suffix", "platform/postgres",
  "add a function segment to the registry", "split a module into its own assembly".
when-to-use: >
  feature filename grammar, feature-cohesive folder, web/features, web/platform, api/features,
  grpc/features, platform cluster, filename grammar, use-case folder, per-use-case folder,
  commands folder, queries folder, TWA0015, TWA0016, feature-filename-grammar.json,
  membership guard, feature-membership.targets, escape hatch filename, function segment,
  layer suffix, registry edit rebuild, per-module assembly split, shared tree vs artifact folder,
  deletion litmus test, where does this file go, seam interface placement
---

# Feature placement and filename grammar

> All logic lives in a concern folder under a shared tree — `features/` for product concerns,
> `platform/` for platform concerns — named by the filename grammar; artifact folders hold only
> the artifact definition (csproj, global-usings) and its entry-point bootstrap (program.cs,
> appsettings, launchSettings, host-config exemplars). The machinery is family-generic — web,
> api, and grpc each get their own `features/`/`platform/`/`msbuild/` trees from the same SSOT
> registry (yarp excepted — single-project family, no concern trees). Worked examples below use
> web; substitute `api/`/`grpc/` for the family root and the same rules apply.

The litmus test for the fuzzy middle:

> **If this deployable were deleted, would the file still mean something?** Yes → shared tree
> (which concern folder?). No → it is bootstrap; it stays with the artifact.

| Home | Use for | Namespace | Examples |
|------|---------|-----------|----------|
| `web/features/<slice>/` | Product concerns: an operation gets its own `<slice>/<use-case>/` folder; a file serving more than one operation (shared contract, store) stays at slice root | `…Features.<Id>` (TWA0009) | `admin/roles/create-role/`, `chat/chat-hub-server.cs` |
| `web/platform/<cluster>/` | Platform concerns: a host/platform cluster split across layers — including a seam interface living beside the implementation it seams with, not sorted into a separate layer folder | Non-Features (e.g. Configuration, Services) | `platform/postgres/`, `platform/identity-host/i-current-principal-accessor-application.cs` + `http-current-principal-accessor-server.cs` |
| `web/projects/<artifact>/` | Artifact (csproj home) under the family `projects/` group — definition (csproj, global-usings) and entry-point bootstrap only; content that would mean nothing if you imagine the deployable gone. Occupants: `web-contracts/`, `web-application/`, `web-domain/`, `web-infrastructure/`, `web-server/`, `web-spa/` | Host assembly defaults | `program.cs`, `sample-options.cs` (binding/validation exemplar, not a real concern) |
| `web/msbuild/` | Build machinery for the web family (filename-grammar props, membership targets) | n/a | `feature-membership.targets` |

**Family root shape:** multi-project container-app families group artifact folders under
`projects/` so the root reads as the placement rule (`features/` + `platform/` + `projects/` +
`msbuild/`). This shape is family-generic — web, api, and grpc each have their own
`features/`/`platform/`/`msbuild/` trees (task 129: all three now hold real content — web's
product/platform slices, api's `weather-forecast/`, grpc's `hello/`/`superhero/`/`greeter/` +
`platform/codegen/`). **yarp** is a single-project family (`yarp/` *is* the project — appsettings
at its root); it is not nested under `projects/` and has no concern trees.

**Folder location is for humans; filename decides project membership.** Each layer project
composes its files with static filename globs keyed to a suffix under its own family's
`{Prefix}FeatureTreeRoot` and `{Prefix}PlatformTreeRoot` (`Web`/`Api`/`Grpc`), not a folder path —
a seam interface's `-application.cs` suffix pulls it into the family's `-application` compilation
unit from wherever it physically sits, which is exactly why it lives beside its `-server.cs`
implementation in `platform/identity-host/` instead of a folder split by layer (the old
`web-application/abstractions/`, retired: conflating layer with
folder was never a principled reason to separate a seam from the concern it belongs to).

**Modules follow concerns, not assemblies.** A module (`IModule`) is a concern's registration
manifest — the DI wiring that concern needs — and lives in the concern's folder like any other
layer file: a product concern's module at its slice root (e.g.
`features/identity/in-memory-identity-stores-module-infrastructure.cs`), a platform concern's
module in its cluster (e.g. `platform/postgres/postgres-db-module-server.cs`). There are no
assembly-level modules — an assembly is a compilation unit, not a concern. The host's
`program.cs` remains the ordered composition root: module *definition* is logic (shared tree);
module *ordering* is bootstrap (artifact folder). A concern with no registrations needs no
module — no ceremony.

Product code for a web container-app lives in **one feature-cohesive folder per slice** —
`web/features/<slice>/` — with every layer (contracts, application, domain, infrastructure,
server) colocated in that folder. Host/platform clusters that are **not** product slices live
under `web/platform/<cluster>/` with the **same** `-layer` filename suffixes (so the same
layer-project globs pick them up) but **without** `…Features.<Id>` namespaces. This is the
answer to "where does this file live" and "what do I name it" — the most common file-placement
decision in the repo.

Inside a slice, files group **by use case**, not by message kind: every operation gets its own
`<slice>/<use-case>/` folder holding all of that operation's layer files side by side — the
contract next to its handler, not sorted into `commands/`/`queries/` subfolders. See
[Use-case folders](#use-case-folders) below.

## Detection — when to invoke

| Signal | How to find it |
|--------|----------------|
| Creating/moving/renaming a `.cs` file under a product slice | any `web/features/<slice>/…` folder |
| `TWA0015` / `TWA0016` diagnostic | analyzer output names a filename and a function segment |
| "Feature file(s) match NO registered layer suffix" build error | membership guard (`feature-membership.targets`) |
| Adding a new archetype (a new hand-authored generation pattern) | needs a new registry function entry |
| "Should this module get its own assembly?" | axis-2 note below |

## Grammar

```
<name>[-<function>]-<layer>.cs
```

| Segment | Required | Meaning |
|---------|----------|---------|
| `name` | yes | operation or slice-local subject, kebab-case (`get-roles`, `hello`) |
| `function` | optional | a **registered** archetype keyword; when present it must pair with exactly one layer (checked at build) |
| `layer` | yes | one of `contracts`, `application`, `domain`, `infrastructure`, `server` — decides which csproj globs the file |

The function segment is a deliberate **two-things-must-agree** seam: naming the archetype in
the filename *and* the layer both, so the analyzer can catch a mismatch instead of relying on
a human remembering the pairing.

### Worked examples per archetype

| Filename | Layer | Function → required layer | Living anchor |
|----------|-------|----------------------------|----------------|
| `create-role-handler-application.cs` | application | `handler` → `application` | `web/features/admin/roles/create-role/create-role-handler-application.cs` |
| *(reserved)* `<name>-endpoint-server.cs` | server | `endpoint` → `server` | registered for a hand-authored server endpoint shim; the template generates FastEndpoints from contracts rather than hand-authoring them, so use this only for a genuinely hand-written endpoint |

A mismatched pairing (e.g. `create-role-handler-server.cs`, function `handler` on layer
`server`) is **TWA0015** — see below.

**Reserved layer headroom:** `domain` is a registered layer with its own csproj glob and
membership-guard entry, but most product slices need only contracts and application (plus
infrastructure or server where relevant) — a slice earns a `-domain.cs` file only once it needs
its own aggregate root (`IAggregateRoot`) rather than a platform/shared one. `domain` stays
registered as intentional headroom for that case, the same way the reserved `endpoint` function
above is kept documented but currently unused.

### Contracts drop the function segment

For contracts, function and layer are the same thing, so writing both would stutter
(`-contract-contracts`). Contract files use `<name>-contracts.cs` with no function segment,
and every `-contracts.cs` file is held to the operation-contract shape (Command/Query,
`[ApiRoute]`, `I*Details`, `Validator` — see `tw-web-api-contracts`):

- `create-role-contracts.cs`
- `get-roles-contracts.cs`
- `role-details-contracts.cs` (shared bindable shape, no function)

### Escape hatch

Not every file is an archetype instance. When a file has no registered function, omit the
function segment entirely — `<name>-<layer>.cs` — and the grammar imposes no archetype shape
on it:

- `role-store-application.cs`
- `web-authn-payload-decoder-application.cs`

An unregistered or misspelled *token that looks like it's trying to be a function* is
**TWA0016**, not a silent escape hatch — see below.

## Use-case folders

The rule is unconditional: **every operation gets its own `<slice>/<use-case>/` folder**
holding every layer file for that operation, side by side — the contract next to its handler.
A folder with only two files in it is correct; there is no size threshold below which an
operation stays flat at slice root. Files that serve **more than one** operation (a shared
bindable DTO, a store, an entity-type configuration) stay at slice root instead of picking one
use-case folder to live in.

`commands/` and `queries/` subfolders (or any other group-by-kind split, such as
`client-to-server/`/`server-to-client/` for a hub) do not appear inside a slice — grouping by
message kind is a layer instinct, and a feature-cohesive slice groups by use case instead.
Folder path never affects project membership (only the filename suffix does — see Grammar
above), so this is a pure human-navigation convention, not something the build enforces.

**Worked example — a whole slice (`web/features/admin/roles/`):**

```text
admin/roles/
  create-role/
    create-role-contracts.cs
    create-role-handler-application.cs
  delete-role/
    delete-role-contracts.cs
    delete-role-handler-application.cs
  get-role/
    get-role-contracts.cs
    get-role-handler-application.cs
  get-roles/
    get-roles-contracts.cs
    get-roles-handler-application.cs
  update-role/
    update-role-contracts.cs
    update-role-handler-application.cs
  role-details-contracts.cs          # shared bindable shape used by every use case above
  role-store-application.cs          # shared store, not operation-specific
```

Every use-case folder here holds exactly one contract file and one handler file — two files is
the normal case, not a special one. `role-details-contracts.cs` and `role-store-application.cs`
each serve multiple use cases (or the whole slice), so they stay at `admin/roles/` root rather
than moving into any single use-case folder.

When a slice's operation name is identical to the slice name (a single-operation slice), the
use-case folder is still literal — `hello/hello/hello-contracts.cs`, not a special-cased flat
layout. When a hub or similar has one folder per message DIRECTION instead of per use case
(`client-to-server/`, `server-to-client/`), that is the same group-by-kind instinct as
`commands/`/`queries/` and collapses the same way: one folder per use case
(`send-message/`, `receive-message/`), not one folder per direction.

## Registry (SSOT)

`source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`:

```json
{
  "layers": [ "contracts", "application", "domain", "infrastructure", "server" ],
  "unroutedLayers": [ "tests" ],
  "functions": {
    "handler": "application",
    "endpoint": "server"
  }
}
```

This JSON is the **single source of truth** and is itself family-agnostic. An MSBuild target on
the convention-analyzers project regenerates it into a family-agnostic artifact plus one
standalone artifact per family before every compile:

- `feature-filename-grammar.g.cs` — analyzer constants (layers, function→layer map, longest-first
  match order) consumed by the TWA0015/TWA0016 analyzer. Generated once (family-agnostic).
- `source/container-apps/{web,api,grpc}/msbuild/feature-filename-grammar.g.props` — one per
  family, each generated from the same JSON by the same generator parameterized with that
  family's prefix (`Web`/`Api`/`Grpc`). Each holds its family's layer list, hybrid `Compile
  Include` globs, and the regex its family's membership guard matches filenames against.

**`unroutedLayers` (task 135):** a registered-but-unrouted layer (currently just `tests`, backing
co-located Jaribu runfiles) is matched and validated by TWA0015/TWA0016 and the membership guard
**exactly like a routed layer** — a `-tests.cs` file is a legitimate archetype, and
`create-role-handler-tests.cs` still trips TWA0015 through the ordinary pairing logic (the
`handler` function still requires `-application`, no matter which layer the file actually ends
in) — but it gets **no `Compile` glob** in any family's `feature-filename-grammar.g.props`, so it
claims no layer project's build. This is what lets a co-located test file live beside real slice
code, stay a first-class grammar citizen (orphaned or misnamed `-tests.cs` files still trip the
teaching membership-guard error), and still compile into nothing. Functions register **only**
against routed layers — `unroutedLayers` entries never appear as a `functions` value.

**Enforcement surface — honest scope:** TWA0015/0016 and the membership guard only see a
`-tests.cs` file when it is actually **compiled as part of some MSBuild invocation**. Because an
unrouted layer claims no layer project's `Compile` glob by design, the repo's own `dev build`
solution gate **never compiles these files** (and aggregators are deliberately not in `.slnx`).
Compile coverage for co-located runfiles comes from:

1. **Standalone** `dotnet build` / `dotnet run` on the runfile (synthesized single-file project).
2. **Family `JARIBU_MULTI` aggregators** (task 136) under
   `tests/container-apps/<family>/<family>-jaribu-tests/` — glob that family's
   `features/**/*-tests.cs` and `platform/**/*-tests.cs`; discovered by `dev test` (MTP bare
   `dotnet test` from the project dir). Web and api exist today; grpc when it gains runfiles.
   **A new aggregator MUST carry a project-local `global.json` with
   `"test": { "runner": "Microsoft.Testing.Platform" }` AND the root SDK pin mirrored** — that
   `global.json` is the sole signal `dev test` keys off to pick the MTP invocation; the csproj's
   `TestingPlatformDotnetTestSupport` property alone is NOT detected, and an aggregator missing
   the file silently falls to the unsupported VSTest path and fails at `dev test` time.
3. **`dev template-smoke`** tiers 1–3 for the two exemplars (guard text, standalone run, aggregator
   MTP counts).

A broken or misnamed co-located test that is never run standalone and is not yet under an
aggregator's glob can still sit undetected by `dev build` alone — prefer `dev test` (or
standalone run) after adding a runfile.

### Co-located Jaribu runfile preamble (the `tests` layer)

This is the canonical in-repo home for the co-located Jaribu runfile authoring convention
(task 135) — the cross-repo `tw-jaribu` skill covers Jaribu itself (test attributes, naming,
assertions) but not this repo-specific preamble; updating it there is tracked as a follow-up, not
duplicated here. Reference implementations:
`source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` (host-free
contract round-trip) and
`source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs`
(real host, fixed port 7255, class-scoped `SetupOnce`/`CleanUpOnce` for host dispose —
requires `TimeWarp.Jaribu` ≥ 1.0.0-beta.14) — read one before writing a new co-located test.

```csharp
#!/usr/bin/env -S dotnet --
#:project <path-to-the-layer-project-this-test-needs, e.g. $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj>
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

#region Purpose
// One honest line: what this runfile proves.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace Your.Slice.Namespace
{
  // Jaribu test classes — see the two exemplars above.
}
```

- `#:property PublishAot=false` — .NET 10 file-based apps default `PublishAot=true`, which bakes
  in reflection-disabling runtime feature switches and breaks
  `ContractSerializationDefaults`-style reflection-based JSON.
- `#:property NoWarn=$(NoWarn);…` — **the `$(NoWarn);` prefix is required.** A bare
  `NoWarn=CA1707;…` literal *replaces* the property rather than appending to it, silently
  un-suppressing everything `Directory.Build.props` already accumulated (CA1052/CA1515/RCS1102
  are already ambient from `source/container-apps/Directory.Build.props` and don't need
  re-listing here; CA1707/CA1849/IDE0161/IDE0021/IDE0058 are the ones this runfile shape needs on
  top of that).
- The `#if !JARIBU_MULTI` / `return` / `#endif` block MUST stay wrapped in the `//-:cnd:noEmit` /
  `//+:cnd:noEmit` escape (TWA0008) — without it, `dotnet new`'s conditional processor strips the
  `#if`/`#endif` directive lines from the generated app's copy while keeping the `return`
  unconditional (task 134 finding M1), breaking the family `JARIBU_MULTI` aggregator build.
  `dev template-smoke` tier 1 regression-tests this for the two exemplars; tier 3 runs the
  generated aggregators via MTP.
- New runfiles that introduce additional `#:project` dependencies must extend the matching
  family aggregator's `ProjectReference` list (`web-jaribu-tests` / `api-jaribu-tests`).
- When co-located **test method totals** change for an exemplar family (or a new family gains
  runfiles), also bump `TemplateSmokeHarness.JaribuFamilyAggregators` expected counts in
  `tools/dev-cli/services/template-smoke-harness.cs` (tier 3 hardcodes succeeded counts —
  web 5 / api 2 today). A green `dev test` alone is not enough if smoke still expects the old
  total.
- `#region Purpose` is never suppressed (TWA0004) — write the real one-line reason, not a
  placeholder.

Adding or changing a function or layer means editing only the JSON — the change applies to every
family:

1. Add the entry (e.g. a new `"validator": "application"` pair, or a new `unroutedLayers` entry).
2. Build the analyzers project (or a full solution build) so both generated files regenerate.
3. **Do a full rebuild, not an incremental one.** Analyzer DLLs can go stale under incremental
   MSBuild — a registry change that doesn't get picked up will silently keep enforcing the old
   pairing. Treat every registry edit as `dev build --clean`-worthy.
4. A layer-suffix that would nest inside another registered suffix (dual-glob-match risk) is
   rejected at generation time — the generator fails the build rather than shipping an ambiguous
   registry. The nesting check covers `layers` **and** `unroutedLayers` together.

## Membership guard

Each family gets its own guard: `<family>/msbuild/feature-membership.targets` (imported once via
`<family>/Directory.Build.targets`) walks every `.cs` under that family's `features/` and
`platform/` trees and requires each one to match exactly one registered `-{layer}` suffix,
generated from the same registry. A file matching **zero** registered suffixes is a **build
error** — it would otherwise compile into no project at all:

> Feature/platform file(s) match NO registered layer suffix and would compile into no project:
> `<file>`. Rename to `<name>[-<function>]-<layer>.cs` with layer one of: `-contracts,
> -application, -domain, -infrastructure, -server`. Registry: `feature-filename-grammar.json`.
> Trees: `features/` and `platform/`.

Fix: rename the file to end in one of the registered layer suffixes. Dual-match (two suffixes
claiming the same file) can't happen structurally once suffix nesting is rejected at generation
time, so this guard's only failure mode in practice is a missing or misspelled layer suffix.

## TWA0015 / TWA0016 — what they mean and how to fix

| Diagnostic | Trigger | Fix |
|------------|---------|-----|
| **TWA0015** | Filename's function segment is registered, but paired with the wrong layer suffix (e.g. `-handler-` on a file ending `-server`) | Rename the file to end in the function's registered layer, or drop the function segment entirely if the file isn't actually that archetype |
| **TWA0016** | Filename's trailing segment looks like a function but isn't registered — an unrecognized token, a misspelling, a case mismatch (`-Handler-` vs `-handler-`), or an incomplete multi-segment function that shares the final segment of a registered multi-segment function without matching it fully | Use a registered function name exactly as spelled/cased, or use the escape-hatch form `<name>-<layer>.cs` with no function segment if the file isn't an archetype instance |

Both diagnostics report the file name, the offending segment, and the full list of registered
pairs/functions so the fix doesn't require opening the registry to look it up.

**Path-matching caution:** these diagnostics only fire on the cohesive tree
(`web/features/…`), never on `web-spa/features/…` (SPA exception below) or on generated
scaffolding. Roslyn can report a glob-included file's path as project-relative with `..`
segments (e.g. `web-server/../features/hello/hello-handler-application.cs`); anything that
scopes analysis to the cohesive tree must normalize such paths rather than matching a bare
project-directory substring, or it risks silently treating the entire cohesive tree — or the
entire SPA tree — as in or out of scope incorrectly.

## Features substrate (cross-slice constants)

Some product files intentionally use the bare `TimeWarp.Architecture.Features` namespace —
**no** slice `…Features.<Id>` — so multiple product slices can reference well-known ids without
cross-slice coupling (TWA0009). This is the **Features substrate** tier, not a product slice.

| Litmus | Home |
|--------|------|
| Compile-time constants or shapes many product slices must share (role ids, module ids) | Bare `…Features` namespace; file still lives under a folder for humans (e.g. `features/authorization/role-ids-contracts.cs`, `features/admin/modules/module-ids-contracts.cs`) |
| Product operation / slice-owned logic | `…Features.<Id>` under `features/<slice>/` |

Document the choice in the file's `#region Design` (existing examples do). Do **not** invent a
grab-bag shared assembly for one-off constants — substrate is for true cross-slice contract data
only. SPA base types under `web-spa/features/base/` also use bare `Features` by SPA convention;
that is separate from the cohesive product-tree substrate above.

## SPA exception

`web-spa/features/` stays **conventionally organized** — one folder per slice, Razor SDK
defaults, no cross-folder glob, no filename-grammar suffix requirement. Razor's own
source-generation and item types make `.razor` a poor fit for the layer-suffix scheme, so the
SPA is deliberately left out of the cohesive-tree rehome. Page/state/action placement inside
`web-spa/features/<slice>/` is a `tw-slice-isolation` question (namespace/tier), not a
filename-grammar one.

## Proto exception

grpc's proto-first artifacts — `.proto` source files (e.g. `greet.proto`) and their generated
`GreeterBase`/message code — stay in their artifact folder (`grpc-server/protos/`) and are
**out of filename-grammar scope entirely**: no `-<layer>` suffix, no cohesive-tree membership,
not scanned by the membership guard or TWA0015/TWA0016. The proto toolchain owns their
compilation and namespace (`option csharp_namespace`); a hand-authored *implementation* of a
proto-generated service (e.g. `GreeterService : Greeter.GreeterBase`) is ordinary product code
and follows the normal rules — it lives in its own `grpc/features/<slice>/<use-case>/
<name>-server.cs` like any other slice, it just happens to inherit from a proto-generated base
class that lives elsewhere. Code-first gRPC contracts (protobuf-net.Grpc `[ServiceContract]`
interfaces, `[DataContract]`/`[ProtoContract]` DTOs) are the opposite case: ordinary C# types
under full grammar scope, `-contracts.cs` like any other contract.

**Code-first service interfaces are not always a free `-application.cs` seam move:** a
protobuf-net.Grpc `[ServiceContract]` interface carries wire-protocol attributes
(`System.ServiceModel`/`Grpc.Core`) that only the contracts project references by default —
moving one to `-application.cs` needs the destination project's own `global-usings.cs` extended
to match (global usings are per-project and do not flow through `ProjectReference`), and, more
importantly, if any consumer outside the family (e.g. a WASM client) references the interface
directly by referencing only `*-contracts.csproj`, moving it to `-application.cs` changes its
compilation unit and breaks that consumer's reference unless the consumer's project reference
also changes — check the consumer graph before applying the seam-interface pattern to a
gRPC service interface, not just for plain seam interfaces.

## Axis-2 note: per-module assembly splits are a glob operation

Implementation layers (application, domain, infrastructure) default to **one assembly per
layer** across all slices; TWA0009 governs module privacy inside that shared assembly via
namespace, not via a compiler-enforced assembly boundary. If a module later earns its own
assembly (it gets large, sensitive, or heads toward service extraction), splitting it out is a
**csproj/glob change, not a file-move** — the new project's `Compile Include` glob simply
narrows to that module's slice folder(s) under the same cohesive tree. Files never move and
namespaces don't change; only which project's glob claims them does.

## Agent workflow

- **Creating a new operation in a slice:** give it its own `<slice>/<use-case>/` folder
  (unconditional — even a two-file folder is correct); pick each file's `layer` from what its
  content actually is (mediator handler → `application`; contract shape → `contracts`;
  FastEndpoint annotation → `server`); add a `function` segment only if the file matches a
  registered archetype exactly; otherwise omit it (escape hatch).
- **Adding a file that serves more than one operation:** it stays at slice root, not inside any
  single use-case folder — a shared details contract, store, or entity-type-configuration file
  is slice-wide, not operation-specific.
- **Moving a file between slices:** rename only the `name` segment (and relocate the folder);
  `function`/`layer` segments don't change unless the file's role changed too. Namespaces are
  never renamed by a folder move (see AGENTS.md).
- **Hitting a build error on a new file:** it almost always means a missing or misspelled
  layer/function suffix — the membership guard and TWA0004 catch misplacement at build time,
  not at file-creation time, so a freshly created file with the wrong name compiles into
  whatever project's default globs happen to claim it until the next build.
- **Extending the registry:** see Registry section above — edit the JSON, rebuild fully, never
  hand-edit the generated `.g.cs`/`.g.props`.

## Related skills and pointers

- `tw-slice-isolation` — product-slice placement and TWA0009 isolation; this skill is the
  filename/layer question once you already know which slice a file belongs to
- `tw-web-api-contracts` — the operation-contract shape that every `-contracts.cs` file must
  satisfy
- `tw-jaribu` (cross-repo) — Jaribu itself (test attributes, naming, assertions); the
  repo-specific co-located runfile preamble for the `tests` registered-unrouted layer lives
  above, in **Co-located Jaribu runfile preamble** — updating the cross-repo skill with a pointer
  to it is tracked as a follow-up, not this skill's job
- **AGENTS.md** — Layout section (cohesive tree diagram) and the TWA diagnostic table
  (TWA0015/TWA0016 rows)
- **Registry (source of truth):**
  `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
- **Analyzer (source of truth):**
  `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar-analyzer.cs`
- **Membership guard (source of truth, one per family):**
  `source/container-apps/{web,api,grpc}/msbuild/feature-membership.targets`
