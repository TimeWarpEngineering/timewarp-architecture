---
name: tw-feature-placement
description: >-
  **TIMEWARP SKILL** — feature-cohesive folder placement and the filename grammar
  `<name>[-<function>]-<layer>.cs` for product code under `web/features/`: which layer a file
  belongs to, what to name it, the registry that backs TWA0015/TWA0016, and the
  membership-guard build errors. Invoke before creating, moving, or renaming a file under a
  feature slice, or when a TWA0015/TWA0016/membership-guard error appears.
  WHEN: "Where does this handler file go?", "What do I name this contract file?",
  "TWA0015", "TWA0016", "feature file matches no registered layer suffix",
  "add a function segment to the registry", "split a module into its own assembly".
when-to-use: >
  feature filename grammar, feature-cohesive folder, web/features, filename grammar,
  TWA0015, TWA0016, feature-filename-grammar.json, membership guard, feature-membership.targets,
  escape hatch filename, function segment, layer suffix, registry edit rebuild,
  per-module assembly split
---

# Feature placement and filename grammar

Product code for a web container-app lives in **one feature-cohesive folder per slice** —
`web/features/<slice>/` — with every layer (contracts, application, domain, infrastructure,
server) colocated in that folder. **Folder location is for humans; filename decides project
membership.** Each layer project composes its files with a static filename glob keyed to a
suffix, not a folder path. This is the answer to "where does this file live" and "what do I
name it" — the most common file-placement decision in the repo.

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
| `create-role-handler-application.cs` | application | `handler` → `application` | `web/features/admin/roles/create-role-handler-application.cs` |
| `hello-feature-annotations-server.cs` | server | `feature-annotations` → `server` | `web/features/hello/hello-feature-annotations-server.cs` |
| *(reserved)* `<name>-endpoint-server.cs` | server | `endpoint` → `server` | registered for a hand-authored server endpoint shim; the template currently generates FastEndpoints from contracts rather than hand-authoring them, so use this only for a genuinely hand-written endpoint |

A mismatched pairing (e.g. `create-role-handler-server.cs`, function `handler` on layer
`server`) is **TWA0015** — see below.

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

## Registry (SSOT)

`source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`:

```json
{
  "layers": [ "contracts", "application", "domain", "infrastructure", "server" ],
  "functions": {
    "handler": "application",
    "endpoint": "server",
    "feature-annotations": "server"
  }
}
```

This JSON is the **single source of truth**. An MSBuild target on the convention-analyzers
project regenerates two artifacts from it before every compile:

- `feature-filename-grammar.g.cs` — analyzer constants (layers, function→layer map, longest-first
  match order) consumed by the TWA0015/TWA0016 analyzer.
- `source/container-apps/web/msbuild/feature-filename-grammar.g.props` — the layer list and the
  hybrid `Compile Include` globs the layer projects use, plus the regex the membership guard
  matches filenames against.

Adding or changing a function or layer means editing only the JSON:

1. Add the entry (e.g. a new `"validator": "application"` pair).
2. Build the analyzers project (or a full solution build) so both generated files regenerate.
3. **Do a full rebuild, not an incremental one.** Analyzer DLLs can go stale under incremental
   MSBuild — a registry change that doesn't get picked up will silently keep enforcing the old
   pairing. Treat every registry edit as `dev build --clean`-worthy.
4. A layer-suffix that would nest inside another registered suffix (dual-glob-match risk) is
   rejected at generation time — the generator fails the build rather than shipping an ambiguous
   registry.

## Membership guard

`web/msbuild/feature-membership.targets` (imported once via `web/Directory.Build.targets`) walks
every `.cs` under the cohesive tree and requires each one to match exactly one registered
`-{layer}` suffix, generated from the same registry. A file matching **zero** registered
suffixes is a **build error** — it would otherwise compile into no project at all:

> Feature file(s) match NO registered layer suffix and would compile into no project: `<file>`.
> Rename to `<name>[-<function>]-<layer>.cs` with layer one of: `-contracts, -application,
> -domain, -infrastructure, -server`. Registry: `feature-filename-grammar.json`.

Fix: rename the file to end in one of the registered layer suffixes. Dual-match (two suffixes
claiming the same file) can't happen structurally once suffix nesting is rejected at generation
time, so this guard's only failure mode in practice is a missing or misspelled layer suffix.

## TWA0015 / TWA0016 — what they mean and how to fix

| Diagnostic | Trigger | Fix |
|------------|---------|-----|
| **TWA0015** | Filename's function segment is registered, but paired with the wrong layer suffix (e.g. `-handler-` on a file ending `-server`) | Rename the file to end in the function's registered layer, or drop the function segment entirely if the file isn't actually that archetype |
| **TWA0016** | Filename's trailing segment looks like a function but isn't registered — an unrecognized token, a misspelling, a case mismatch (`-Handler-` vs `-handler-`), or an incomplete multi-segment function (e.g. `feature-only-annotations-server.cs` shares the `annotations` tail of the registered `feature-annotations` function but isn't that function) | Use a registered function name exactly as spelled/cased, or use the escape-hatch form `<name>-<layer>.cs` with no function segment if the file isn't an archetype instance |

Both diagnostics report the file name, the offending segment, and the full list of registered
pairs/functions so the fix doesn't require opening the registry to look it up.

**Path-matching caution:** these diagnostics only fire on the cohesive tree
(`web/features/…`), never on `web-spa/features/…` (SPA exception below) or on generated
scaffolding. Roslyn can report a glob-included file's path as project-relative with `..`
segments (e.g. `web-server/../features/hello/hello-handler-application.cs`); anything that
scopes analysis to the cohesive tree must normalize such paths rather than matching a bare
project-directory substring, or it risks silently treating the entire cohesive tree — or the
entire SPA tree — as in or out of scope incorrectly.

## SPA exception

`web-spa/features/` stays **conventionally organized** — one folder per slice, Razor SDK
defaults, no cross-folder glob, no filename-grammar suffix requirement. Razor's own
source-generation and item types make `.razor` a poor fit for the layer-suffix scheme, so the
SPA is deliberately left out of the cohesive-tree rehome. Page/state/action placement inside
`web-spa/features/<slice>/` is a `tw-slice-isolation` question (namespace/tier), not a
filename-grammar one.

## Axis-2 note: per-module assembly splits are a glob operation

Implementation layers (application, domain, infrastructure) default to **one assembly per
layer** across all slices; TWA0009 governs module privacy inside that shared assembly via
namespace, not via a compiler-enforced assembly boundary. If a module later earns its own
assembly (it gets large, sensitive, or heads toward service extraction), splitting it out is a
**csproj/glob change, not a file-move** — the new project's `Compile Include` glob simply
narrows to that module's slice folder(s) under the same cohesive tree. Files never move and
namespaces don't change; only which project's glob claims them does.

## Agent workflow

- **Creating a new file in a slice:** pick `layer` from what the file's content actually is
  (mediator handler → `application`; contract shape → `contracts`; FastEndpoint annotation →
  `server`); add a `function` segment only if the file matches a registered archetype exactly;
  otherwise omit it (escape hatch).
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
- **AGENTS.md** — Layout section (cohesive tree diagram) and the TWA diagnostic table
  (TWA0015/TWA0016 rows)
- **Registry (source of truth):**
  `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
- **Analyzer (source of truth):**
  `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar-analyzer.cs`
- **Membership guard (source of truth):**
  `source/container-apps/web/msbuild/feature-membership.targets`
