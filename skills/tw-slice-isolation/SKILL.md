---
name: tw-slice-isolation
description: >-
  **TIMEWARP SKILL** — product slice placement and TWA0009 isolation (SliceRoot,
  namespaces, platform Applications, Components/contracts sharing,
  CrossSliceReference opt-out). Invoke before scaffolding a new feature/slice/page
  or when fixing TWA0009 / cross-slice references.
  WHEN: "Add a new clients feature page", "Where does this state live?",
  "TWA0009 slice references another product slice", "CrossSliceReference",
  "new product area under features/", greenfield slice scaffolding.
when-to-use: >
  slice, slice isolation, TWA0009, CrossSliceReference, SliceRoot, TimeWarpSliceRoot,
  product feature folder, features/, Applications platform, cross-slice, where page/state live
---

# Slice isolation (TWA0009)

Slices are **independently removable vertical units**. Folders organize humans; the
**namespace under SliceRoot is the law**. Analyzer **TWA0009** is the reactive safety net;
this skill is for **correct first placement** so greenfield work does not couple product
areas by accident.

Enforcement ships in `TimeWarp.Architecture.Analyzers` (and monorepo ProjectReference). Do not
re-interpret rules here — mirror the analyzer.

## Detection — when to invoke

| Signal | How to find it |
|--------|----------------|
| Product area under `features/` | SPA: `web-spa/features/…`; server product code: `web/features/<slice>/` (axis-1 cohesive tree; layer projects glob `*-{layer}.cs`) |
| Namespace under SliceRoot | `…Features.<Id>` (or nested `…Features.Admin.Roles`) |
| TWA0009 / `CrossSliceReference` | diagnostic text or attribute on a type |
| “Where does this page/state live?” | any new product capability before scaffolding |

## Terms

| Term | Meaning |
|------|---------|
| **Slice** | Removable vertical capability; the isolation unit |
| **Feature** | Informal product language only — not the analyzer’s unit name |
| **Module** | `IModule` / host DI composition — **not** a product slice |

Prefer saying **slice** in diagnostics, opt-outs, and agent prose. Namespace path may still use
`…Features.*` (familiar VSA / contracts layout).

## SliceRoot

| Setting | Value |
|---------|--------|
| Default | `{RootNamespace}.Features` (from MSBuild `RootNamespace`) |
| Override | MSBuild property `TimeWarpSliceRoot` (must be `CompilerVisibleProperty`) |

**Slice id** = path under SliceRoot after stripping structural suffixes `Pages`, `Components`,
`Application`. Nested ids are the **full path**:

| Namespace | Slice id / tier |
|-----------|-----------------|
| `…Features` | **Substrate** (bare root) |
| `…Features.Applications` | **Platform** id `Applications` |
| `…Features.Counters` | **Product** id `Counters` |
| `…Features.Counters.Pages` | **Product** id `Counters` (suffix stripped) |
| `…Features.Admin.Roles` | **Product** id `Admin.Roles` |
| `…Components`, layouts, app root types | **Outside** SliceRoot |

Folders usually mirror slices (`features/counter/` ↔ `…Features.Counters`) but **moving a file
does not change legal dependencies** — only the namespace does.

## Tiers (dependency rules)

| Tier | Example | May reference |
|------|---------|---------------|
| **Outside** | `…Components`, shell, layouts | Anything (composition free) |
| **Substrate** | bare `…Features` (`BaseComponent`, shared base types) | Outside, substrate, platform — **not** product without opt-out |
| **Platform** | `…Features.Applications` | Outside, substrate, platform — **not** product without opt-out |
| **Product** | `…Features.Counters`, `…Features.Admin.Roles` | Outside, substrate, platform, **own** product id, contracts (other assembly). **Not** another product id without opt-out |

**Forbidden without opt-out (TWA0009):**

- product A → product B (A ≠ B)
- platform → product
- substrate → product

**Free by design:**

- product → platform (`Applications`) — e.g. `CounterPage` → `ApplicationState`
- any code → **other assemblies** (contracts are sharing-by-design)
- Outside → product (nav shell, shared components)

## Placement matrix

| Artifact | Where |
|----------|--------|
| Page, state, actions, slice-local UI | **Inside** the product slice namespace |
| App chrome / shell / empty `MainLayout` | **Outside** SliceRoot (e.g. `…Components`) — see `tw-blazor-layout` |
| Shared UI used by multiple slices | `components/` → namespace outside SliceRoot |
| Shared API shapes | **Contracts** project (other assembly; free under TWA0009) |
| Host-wide modal / menu / branding state | Platform `Applications` |

Living anchors (timewarp-architecture template):

| Role | Path | Namespace |
|------|------|-----------|
| Product | `web-spa/features/counter/` | `…Features.Counters` |
| Nested product | `web-spa/features/admin/roles/` | `…Features.Admin.Roles` |
| Platform | `web-spa/features/application/` | `…Features.Applications` |
| Substrate | `web-spa/features/base/base-component.cs` | `…Features` |
| Outside shell | `web-spa/components/TimeWarpPage.razor` | `…Components` |

## Greenfield scaffold workflow

1. **Name the slice** — plural, domain-oriented (`Clients`, `Admin.Roles`).
2. **Folder** — kebab path under the project’s `features/` tree (match sibling casing).
   - **SPA** UI stays under `web-spa/features/<slice>/` (conventional; not rehomed by axis-1).
   - **Contracts / application / server** product files live in the cohesive tree
     `web/features/<slice>/` with filename grammar `<name>[-<function>]-<layer>.cs`
     (see `tw-feature-placement` for the full grammar and registry).
3. **Namespace** — `{RootNamespace}.Features.{SliceId}` (plural segments; nested with `.`).
   Folders rehome freely; **namespaces are not renamed with folder moves**.
4. **Colocate** page, state, actions, and slice-private components in that namespace (pages are
   **not** shared infrastructure — no grab-bag `…Pages` for product UI).
5. **Contracts** — same plural slice path under `web/features/<slice>/` with `-contracts.cs`
   suffix (`tw-web-api-contracts` skill). Other assembly → free across SPA slices.
6. **Wire nav / DI** from Outside or platform — not from another product slice.
7. **Prefer share** (Components / contracts) over `[CrossSliceReference]`.
8. **Build** — fix TWA0009 by relocating code first; mute only for deliberate demo/integration edges.

## Share vs opt-out

### Prefer share

- Promote shared controls to `…Components`.
- Share API shapes via contracts assembly.
- Depend product → platform without attributes when the dependency is real host state.

### Opt-out — deliberate edges only

```csharp
using TimeWarp.Foundation.Features;

[CrossSliceReference(typeof(CounterState), "Living style guide exercises the counter throw-exception pipeline.")]
partial class StyleGuidePage;
```

Rules:

- Attribute on the **referencing** type (`AllowMultiple = true` for multiple foreign slices).
- `typeof(T)` maps to the **target product slice**; only edges into that slice are suppressed.
- Reason must be non-empty (human paperwork).
- Partial-safe (semantic attributes).
- Type: `TimeWarp.Foundation.Features.CrossSliceReferenceAttribute` (package
  `TimeWarp.Foundation.Contracts` / monorepo foundation-contracts).

Living examples:

- Style guide → Counters: `web-spa/features/style-guide/pages/StyleGuidePage.razor.cs`
- Auth pipeline deliberate edges: authentication code-behinds with multiple
  `[CrossSliceReference(...)]` when needed

## Limits agents must know

| Limit | Implication |
|-------|-------------|
| **Same assembly only** | Contracts / NuGet metadata types never trip TWA0009 |
| **Hand-written C#** | `GeneratedCodeAnalysisFlags.None` — pure `.razor` without `.razor.cs` is not scanned; put deliberate edges on code-behind |
| **Folders can lie** | Namespace still owns the type |
| **Structural suffixes** | `…Counters.Pages` is still product id `Counters` |

## Good / bad examples

| Bad | Why | Fix |
|-----|-----|-----|
| `…Features.Counters` type uses `WeatherForecastsState` | product → product | Share via Components/contracts, or reasoned `[CrossSliceReference]` |
| Product page in grab-bag `…Pages` outside any slice | not removable with the capability | Namespace under `…Features.<Id>` |
| Platform type reaches into `Counters` | platform ↛ product | Invert dependency (product → platform) or rare opt-out on platform type |
| Mute TWA0009 globally / without reason | hides real coupling | Relocate or scoped opt-out |

| Good | Why |
|------|-----|
| `CounterPage` uses `ApplicationState` | product → platform free |
| `TimeWarpPage` in `…Components` | Outside composition free |
| `Admin.Roles` only uses own types + contracts + platform | nested product isolation |

## Agent checklist

- [ ] New capability has a slice id and matching `{Root}.Features.{Id}` namespace
- [ ] Page, state, and actions live in that namespace
- [ ] Shell/layout stay outside SliceRoot (or platform only when truly host state)
- [ ] Cross-slice data via Components or contracts first
- [ ] Any remaining edge has `[CrossSliceReference(typeof(T), "reason")]`
- [ ] No silent coupling or unexplained suppressions

## Related skills and pointers

- `tw-feature-placement` — filename grammar and layer membership once you know which slice a
  file belongs to (`<name>[-<function>]-<layer>.cs`, TWA0015/TWA0016, membership guard)
- `tw-web-api-contracts` — contract placement; **contracts assemblies are free** under TWA0009; still use plural `…Features.*` aligned with SPA product slices
- `tw-blazor-layout` — empty layout + shell; chrome **outside** SliceRoot
- `tw-blazor-css-strategy` — shell/component styling only
- `tw-agent-context-regions` — Purpose/Design on new files (TWA0004)
- **AGENTS.md** — TWA diagnostic table (row TWA0009)
- **HowToRemoveDemoFeatures** — delete Counter/EventStream demo slices
- **Analyzer (source of truth):** `source/analyzers/timewarp-architecture-convention-analyzers/slice-isolation-analyzer.cs`
- **Opt-out attribute:** `source/foundation/foundation-contracts/base/cross-slice-reference-attribute.cs`
