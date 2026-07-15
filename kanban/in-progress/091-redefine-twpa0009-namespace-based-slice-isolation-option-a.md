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

- [ ] ADR or Design region: lock SliceRoot name (`Features` vs `Slices`), nested-slice policy,
      platform one-way vs reserved platform slice
- [ ] Redefine TWPA0009 ownership/membership to namespace under SliceRoot (drop folder `FeatureOf`)
- [ ] Rehome SPA product pages (and other non-slice namespaces under slice trees) into slice ns
- [ ] Rename/reshape opt-out attribute: typeof + reason, edge-scoped, partial-safe, empty reason ban
- [ ] Fix generic-name walk; keep contracts/metadata exempt
- [ ] Rewrite analyzer tests; live-fire negative control
- [ ] Update AGENTS.md, HowToRemoveDemoFeatures (or rename), contracts how-to if namespace root changes
- [ ] Reconcile Purpose/Design regions on touched files from this task’s Purpose/Design
- [ ] `dev build` 0/0; generation still ships clean template content

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

## Session

- Created: 2026-07-15 (design conversation: Option A, slice vs feature, pages-as-slice-UI, typeof opt-out)
