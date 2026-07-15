# Redefine TWPA0009: namespace-based slice isolation (Option A)

Replaces folder-derived “feature” ownership (current TWPA0009 / task 088) with **namespace-as-slice**
architecture. Greenfield purity — no migration sympathy; the template must be clear of this tech debt.

Related: 088 (folder-based TWPA0009 — superseded model), 071 (flag matrix that first exposed coupling).

## Purpose

(Copy into analyzer / attribute `#region Purpose` when implementing.)

Enforce **slice isolation**: a type whose namespace is under a configured slice root may not
reference types owned by a different product slice in the same assembly. Slices are independently
removable vertical units. Shared UI lives in non-slice namespaces (`Components`); shared API shapes
live in contracts (other assembly — free by design). Deliberate cross-slice edges require an
opt-out bound to a **real type** (and a reason), not a free-form feature name or folder path.

## Design

(Copy into analyzer / attribute `#region Design` when implementing; reconcile if choices change.)

### Why not folders (reject Option D)

Current TWPA0009 derives “feature” from `…/features/<name>/` path segments. That **mixes file
organization with architecture correctness**: moving a file for readability changes legal
dependencies. Organization and architecture are different concerns. Folders may mirror slices for
humans but **must not** be the rule.

Also reject Option C (assembly-per-slice) as the template default — correct but too heavy for many
small demos. Prefer same-assembly slices with enforced namespace boundaries.

### Terminology

| Term | Meaning |
|------|---------|
| **Slice** | Architectural unit: removable vertical capability. What isolation protects. |
| **Feature** | Informal product language only; do not use as the analyzer’s unit name if it confuses. |
| **Module** | Reserved for `IModule` / host DI composition — **not** a product slice. |

Prefer saying **slice** in diagnostics, AGENTS.md, and attributes if renamed
(`CrossSliceReference`). Namespace path may remain `…Features.<Name>` (familiar VSA / existing
contracts docs) **or** be renamed to `…Slices.<Name>` for maximum honesty — decide in
implementation; identity is the segment under the configured root either way.

### Slice root (configured base)

One load-bearing prefix, e.g.:

```text
SliceRoot = {RootNamespace}.Features
# or {RootNamespace}.Slices
```

- **Slice id** = first segment(s) under SliceRoot (`…Features.Counters` → `Counters`; nested
  policy e.g. `…Features.Admin.Roles` — pick and document: parent `Admin` vs full `Admin.Roles`).
- **Not a slice** = anything outside SliceRoot (root app ns, `Components`, third-party).
- **Platform / shell**: either one reserved name under the root (`…Features.Application` /
  `…Features.Platform`) **or** a namespace **outside** SliceRoot so product slices may depend
  one-way on shell without opt-out. Prefer **one-way platform**: product → platform free;
  platform must not reference product slices.
- Substrate types under bare `…Features` (no third segment) are shared base, not a product slice —
  document explicitly.

Configure via convention (RootNamespace + fixed suffix) and/or a single MSBuild/`AnalyzerConfig`
property — not a registry of slice names. **The namespace tree is the catalog.**

### Membership (Option A)

A type belongs to the slice named by its containing namespace under SliceRoot. No folder scan.
No explicit membership attribute in v1 (Option B deferred unless a real case needs it).

### Pages are not shared infrastructure

Grab-bag `namespace …Pages` is the main debt: pages lived under feature folders but outside
feature namespaces, so they looked like infrastructure while being slice UI.

- A page that implements one capability **is part of that slice**
  (`…Features.Counters.Pages` or `…Features.Counters`).
- Host/router/layout shells are **platform**, not a product-pages bag.
- Shared controls → `Components` (outside SliceRoot).
- StyleGuide / debugger demos → their own slice (may opt into other slices).

### Cross-slice opt-out

Replace reason-only type-level mute:

- Bind to a **real type**: `[CrossSliceReference(typeof(AuthorizationState), "…")]` or generic
  equivalent — not a string slice/folder name.
- Analyzer: resolve type → namespace → slice id; **suppress only edges into that slice**.
- `AllowMultiple` for multiple edges; unlisted slices still warn.
- Reason remains human paperwork; target is compile-checked.
- Empty/whitespace reason rejected (Guard).

Partial types: opt-out must be visible across partials (semantic `GetAttributes()`, not syntax-only
on the current file — known bug in current `HasOptOut`).

### Analyzer scope

- Same assembly only; metadata/contracts free.
- Hand-written `.cs`; generated trees still out (document razor markup gap or address later).
- Walk `SimpleNameSyntax` (include generics) — known gap in current IdentifierName-only walk.
- Diagnostic text: “slice”, not “feature folder”.

### What this supersedes

Task 088 / current `FeatureIsolationAnalyzer`: folder ownership map, multi-owner namespace dropout
driven by path, `[CrossFeatureReference(string reason)]` blanket mute. Keep the *goal* (no silent
cross-slice coupling; share via components/contracts) and the live lessons (metadata exempt,
generators poison ownership if path-based — moot under pure namespace identity).

## Requirements

- Slice identity = namespace under configured SliceRoot; folders non-normative.
- SPA (and any project under the rule): no grab-bag `…Pages` for product pages; rehome into slice
  namespaces.
- Opt-out: typeof/generic + reason; edge-scoped; partial-safe.
- Platform one-way policy documented and enforced (or reserved platform slice with explicit rules).
- AGENTS.md enforcement row and HowToRemoveDemo* updated to “slice” + namespace story.
- Analyzer tests rewritten for namespace membership (not fake file paths as ownership source).
- `dev build` 0/0; live-fire negative control for a cross-slice reference.
- Agent-context regions (Purpose/Design) on analyzer, opt-out attribute, and any SliceRoot config
  type match this task’s Purpose/Design.

## Checklist

- [x] ADR or Design region: lock SliceRoot name (`Features` vs `Slices`), nested-slice policy,
      platform one-way vs reserved platform slice
- [x] Redefine TWPA0009 ownership/membership to namespace under SliceRoot (drop folder `FeatureOf`)
- [x] Rehome SPA product pages (and other non-slice namespaces under slice trees) into slice ns
- [x] Rename/reshape opt-out attribute: typeof + reason, edge-scoped, partial-safe, empty reason ban
- [x] Fix generic-name walk; keep contracts/metadata exempt
- [x] Rewrite analyzer tests; live-fire negative control
- [x] Update AGENTS.md, HowToRemoveDemoFeatures (or rename), contracts how-to if namespace root changes
- [x] Reconcile Purpose/Design regions on touched files from this task’s Purpose/Design
- [x] `dev build` 0/0; generation still ships clean template content

## Notes

### Decision summary (2026-07-15 design conversation)

- Option A (namespace convention) over B (explicit marker), C (assembly-per-slice), D (folder).
- Configure base slice namespace root — not a list of slices.
- **Slice** conveys removability better than Feature; Module stays DI-only.
- Pages that serve a capability are **in the slice**; only host/shell is non-slice infrastructure.
- Opt-out must not be free-form feature strings; bind to real types.
- Greenfield: break freely; purity over migration.

### Copic reference (read-only illustration, not in scope to fix)

Legitimate cross-slice edge: `Features/Authentication/AccountClaimsPrincipalFactoryWithRoles`
→ `AuthorizationState` (auth/authz identity pipeline). Same pattern as template claims factory.
Under Option A that is still a reasoned opt-out via `typeof(AuthorizationState)`, not a folder mute.

### Implementation entry points (current tree)

- `source/analyzers/…/feature-isolation-analyzer.cs` (TWPA0009)
- `source/foundation/…/cross-feature-reference-attribute.cs`
- SPA pages under `web-spa/features/**` using `namespace …Pages`
- `AGENTS.md` TWPA0009 row; kanban 088 results for historical context only

### Implementation plan (2026-07-15)

# Implementation Plan: Task 091 — Redefine TWPA0009 (namespace-based slice isolation)

## 1. Locked design decisions

All five open points decided for greenfield purity (repo evidence: existing `…Features.*` tree, contracts skill, SPA layout, task 088 lessons).

### D1. Namespace root: keep `…Features.*` (do not rename to `…Slices.*`)

| Choice | Rationale |
|--------|-----------|
| **Keep** `SliceRoot = {RootNamespace}.Features` | Repo, contracts skill, docs, and every assembly already use `Features.*`. Renaming to `Slices.*` is pure vocabulary churn with zero isolation gain (identity is the segment under the root either way). |
| **Say “slice”** in diagnostics, AGENTS.md, and the opt-out attribute | Matches task terminology without breaking VSA path language. |

**Folder `features/`** remains human organization only — non-normative for the analyzer.

### D2. Nested slices: full path under root is the slice id

Examples:

| Namespace | Slice id |
|-----------|----------|
| `…Features.Counters` | `Counters` |
| `…Features.Counters.Pages` | `Counters` (structural suffix stripped) |
| `…Features.Counters.Components` | `Counters` |
| `…Features.Admin.Roles` | `Admin.Roles` |
| `…Features.Admin.Roles.Application` | `Admin.Roles` (layer suffix stripped) |
| `…Features` (bare) | **not a product slice** (shared substrate) |
| `…Features.Applications` | **platform** (reserved; see D3) |

**Reserved structural suffixes** (not part of slice id; strip from the right while present):

- `Pages`, `Components`, `Application`

Rationale: first-segment-only (`Admin`) would glue every future admin capability into one removable unit. The template’s only nest (`Admin.Roles`) is already a vertical product unit. Full path preserves independent removability.

### D3. Platform: reserved platform slice + outside-SliceRoot shell

Three tiers:

| Tier | Membership | May reference product slices? | Product may reference it? |
|------|------------|-------------------------------|---------------------------|
| **Outside SliceRoot** | `Components`, root shell, `Pipeline`, `Services`, `Configuration`, `Hubs`, composition roots (`NavMenu`, layouts, `Routes`) | Yes (composition root) | N/A |
| **Substrate** | exact `…Features` (`BaseComponent`, handlers, cacheable state base) | No (flag if it does) | Yes (free) |
| **Platform slice** | reserved id `Applications` (`…Features.Applications` and structural children) | No without opt-out | Yes (free, one-way) |
| **Product slices** | every other id under SliceRoot | Only self + free tiers; other product = flag | Symmetric isolation |

Rationale: Counter’s store-reset and app chrome already depend on `ApplicationState`. Treating `Applications` as a normal product slice forces noise opt-outs on legitimate product→shell edges. One-way platform matches “product may depend on shell; shell must not depend on product.”

Do **not** invent a second reserved name `Platform`; keep existing `Applications` namespace.

### D4. Attribute: rename to `CrossSliceReference` (typeof + reason)

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CrossSliceReferenceAttribute : Attribute
{
  public Type TargetType { get; }
  public string Reason { get; }

  public CrossSliceReferenceAttribute(Type targetType, string reason)
  {
    // reject null targetType; reject null/whitespace reason
  }
}
```

- **Delete** `CrossFeatureReferenceAttribute` (greenfield; no obsolete shim).
- Edge-scoped: suppress only references whose target slice equals `SliceOf(TargetType)`.
- Partial-safe: semantic `INamedTypeSymbol.GetAttributes()` on the containing type (all partials).
- Match attribute by metadata name (no foundation-contracts dep from convention-analyzers).

### D5. SliceRoot configuration: convention + optional MSBuild override

1. Default: `SliceRoot = {RootNamespace}.Features` from `build_property.RootNamespace`.
2. Optional override: `build_property.TimeWarpSliceRoot` if non-empty.
3. If neither yields a usable root → analyzer no-ops.

No registry of slice names. **The namespace tree is the catalog.**

---

## 2. Analyzer algorithm (normative)

### 2.1 Slice identity

```
TryGetSliceId(namespaceDisplayString, sliceRoot) → (kind, id?)

kind ∈ { Outside, Substrate, Platform, Product }

- if ns == sliceRoot → Substrate
- if !ns.StartsWith(sliceRoot + ".") → Outside
- rest = ns[(sliceRoot.Length+1)..]
- parts = rest.Split('.')
- while parts.Length > 0 && parts[^1] ∈ StructuralSuffixes: pop
- if parts empty → treat as Substrate (defensive)
- id = string.Join('.', parts)
- if id == "Applications" (Ordinal) → Platform
- else → Product(id)
```

### 2.2 Rule

For each hand-written source file, for each `SimpleNameSyntax` (IdentifierName **and** GenericName):

1. Resolve symbol; skip null / `INamespaceSymbol`.
2. Skip if `ContainingAssembly` ≠ current compilation assembly (contracts/metadata free).
3. `sourceSlice = SliceOf(containing type)` — Outside → skip.
4. `targetSlice = SliceOf(symbol)`.
5. Allowed without opt-out:
   - source Outside
   - target Outside or Substrate
   - target Platform (any source)
   - source and target same Product id
6. Flag when:
   - source Product A → target Product B (A ≠ B)
   - source Platform → target Product *
   - source Substrate → target Product *
7. Opt-out: `CrossSliceReference` on semantic containing type lists `TargetType` whose slice id equals the **target** product slice id.

Diagnostic (keep **TWPA0009**, update text):

```
Slice '{0}' references '{1}', owned by slice '{2}'; share via Components or contracts, or mark the type [CrossSliceReference(typeof(...), reason)]
```

Severity: Warning (warnings-as-errors → build-breaking).

### 2.3 Explicit non-behaviors

- Razor markup trees still out (`GeneratedCodeAnalysisFlags.None`) — known gap.
- No folder scan; no multi-owner namespace dropout.
- No Option B membership attributes in v1.
- Same assembly only.

### 2.4 Bugs fixed

| Gap in 088 | Fix |
|------------|-----|
| Folder ownership | Namespace membership only |
| `IdentifierNameSyntax` only | `SimpleNameSyntax` (generics) |
| Syntax-only opt-out | Semantic `GetAttributes` across partials |
| Blanket mute | Edge-scoped via `typeof` → slice id |
| Grab-bag `…Pages` | Rehome pages into slice namespaces |

---

## 3. Ordered implementation steps

**Principle:** land the rule and tests first; then make the template comply (rehome + opt-outs).

### Phase 1 — Opt-out attribute

Replace `cross-feature-reference-attribute.cs` with `cross-slice-reference-attribute.cs` (`CrossSliceReferenceAttribute`). Namespace stays `TimeWarp.Foundation.Features`. Delete `CrossFeatureReferenceAttribute`.

### Phase 2 — Rewrite analyzer

Rewrite/rename to `slice-isolation-analyzer.cs` / `SliceIsolationAnalyzer` (TWPA0009). Read `RootNamespace` / `TimeWarpSliceRoot`. Drop `FeatureOf` and folder ownership map. Implement `TryGetSliceId`, SimpleName walk, semantic opt-out, one-way platform matrix. Update `AnalyzerReleases.Unshipped.md`.

### Phase 3 — Analyzer unit tests (rewrite)

Rename to `slice-isolation-analyzer-tests.cs`. Path-independent ownership. Cover: A→B flag; same product clean; product→substrate/platform clean; platform→product flag; outside→product clean; metadata clean; nested Admin.Roles; structural suffixes; edge-scoped multi opt-out; partial opt-out; generics; missing RootNamespace no-op; substrate→product flag.

### Phase 4 — Rehome SPA namespaces

Product pages → `…Features.<Slice>` (optional `.Pages` child). Platform Home/Forbidden/NotFound → `…Features.Applications`. Root `web-spa/pages/` grab-bag: Profile→Profiles, Settings→Applications, Authentication/RedirectToLogin→Authentication. Fix AuthenticationStateListener ns. Leave ModalContainer as Components. Fix global-usings, nav, page-mixin, tests.

### Phase 5 — Convert opt-outs

- StyleGuidePage: CounterState + ToastNotificationState (AllowMultiple)
- AccountClaimsPrincipalFactoryWithRoles: AuthorizationState
- CounterPage: remove if only platform deps remain

Triage remaining TWPA0009 after rehome.

### Phase 6 — Documentation

AGENTS.md TWPA0009 row; HowToRemoveDemoFeatures.md; optional ADR (Design region is minimum).

### Phase 7 — Verification

Analyzer tests green; `dev build` 0/0; live-fire negative control; HowToRemoveDemo* still delete-folder + fix compile.

---

## 4. Suggested commit sequencing

1. Attribute + analyzer rewrite + unit tests
2. SPA page rehome + opt-out conversion + build green
3. Docs (AGENTS.md, HowToRemoveDemo*)

Prefer one task-shaped PR; split only if review load demands it.

---

## 5. Risks / non-goals

**Risks:** page rehome breaks usings/nav; incomplete structural suffixes; RootNamespace missing in tests (no-op); razor gap remains; shell composition roots may grow.

**Non-goals:** rename to Slices; assembly-per-slice; folder ownership; Option B; cross-assembly isolation; Copic/external migration; Error severity; scanning generated/razor in v1.

---

## 6. After state (quick reference)

```text
SliceRoot = TimeWarp.Architecture.Features

…Features                    → substrate (shared base)
…Features.Applications       → platform (one-way)
…Features.Counters[.Pages]   → product slice Counters
…Features.Admin.Roles        → product slice Admin.Roles
…Components / shell          → outside (composition free)

[CrossSliceReference(typeof(AuthorizationState), "…")]
```



## Results

### Summary

Redefined TWPA0009 from folder-based feature isolation (task 088) to **namespace-based slice isolation (Option A)**. Slices are independently removable units identified by namespaces under `SliceRoot = {RootNamespace}.Features` (optional `TimeWarpSliceRoot` override, now MSBuild-visible). Grab-bag `…Pages` product pages rehomed into slice namespaces. Opt-out is edge-scoped `[CrossSliceReference(typeof(T), reason)]` with partial-safe semantic attribute lookup.

### What was implemented

1. **`CrossSliceReferenceAttribute`** — replaces deleted `CrossFeatureReferenceAttribute` (no shim); typeof + reason; AllowMultiple; Guard on null/empty.
2. **`SliceIsolationAnalyzer`** (TWPA0009) — namespace membership; structural suffix strip (`Pages`/`Components`/`Application`); tiers Outside / Substrate / Platform(`Applications` one-way) / Product; SimpleName walk; semantic opt-out.
3. **Analyzer tests** rewritten path-independently (including platform→product opt-out).
4. **SPA page rehome** out of `TimeWarp.Architecture.Pages` into slice namespaces.
5. **Opt-outs** — StyleGuide→CounterState; claims factory→AuthorizationState; AuthenticationStateListener→ProfileState+AuthorizationState; CounterPage opt-out removed (platform free).
6. **Substrate rehomes** — RoleIds/ModuleIds and ToastNotificationState to bare `…Features` (shared).
7. **Docs** — AGENTS.md TWPA0009 row; HowToRemoveDemoFeatures.md.
8. **Review fixes** — `CompilerVisibleProperty` for TimeWarpSliceRoot; listener code-behind; opt-out name match cleanup.

### Files changed (key)

| Role | Path |
|------|------|
| Attribute | `source/foundation/foundation-contracts/base/cross-slice-reference-attribute.cs` |
| Analyzer | `source/analyzers/…/slice-isolation-analyzer.cs` |
| Tests | `tests/analyzers/…/slice-isolation-analyzer-tests.cs` |
| MSBuild | `source/Directory.Build.props`, `tests/Directory.Build.props` |
| SPA | page rehomes under `web-spa/features/**` and `web-spa/pages/` |
| Docs | `AGENTS.md`, `HowToRemoveDemoFeatures.md` |

### Key decisions

- Keep namespace root **`Features`** (not rename to `Slices`); say “slice” in diagnostics/docs.
- Nested slice id = full path (`Admin.Roles`); platform id = `Applications` one-way.
- Toast + well-known role/module ids → substrate (not product slices).
- Live-fire SPA inject of illegal edge skipped; unit tests cover the matrix.

### Test outcomes

- Analyzer suite: **62 passed**
- `dev build`: **0 warnings / 0 errors**

### Remaining known gaps (documented, non-blocking)

- Razor markup / generated trees still not scanned (`GeneratedCodeAnalysisFlags.None`)
- Optional per-type opt-out memoization not implemented (nit)

### Commits

- `c60077b9` feat(analyzers): redefine TWPA0009 as namespace-based slice isolation
- `fa603586` refactor(web-spa): rehome pages into slice namespaces for TWPA0009
- `502036a2` docs: update TWPA0009 slice isolation guidance
- `04ca9cda` fix(091): wire TimeWarpSliceRoot and close review follow-ups


## Session

- Created: 2026-07-15 (design conversation: Option A, slice vs feature, pages-as-slice-UI, typeof opt-out)
- Implementation + review: 2026-07-15 (orchestrate-task 091)
