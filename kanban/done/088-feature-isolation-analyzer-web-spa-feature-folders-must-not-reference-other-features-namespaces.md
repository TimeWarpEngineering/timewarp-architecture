# Feature-isolation analyzer: web-spa feature folders must not reference other features' namespaces

Realizes [[prefer-analyzers-sourcegen-over-inference]] for cross-feature coupling.

## Why

The 071 flag-verification loop found cross-feature coupling only because it generated and built
every combination (superhero -> weather-forecast's TableHeader/Cell; StyleGuidePage ->
CounterState; shared pipeline tests -> CounterState). With counter/eventstream de-flagged, the
loop no longer exercises intra-web coupling at all — a build-time analyzer replaces it and runs
on EVERY build instead of once per manual matrix.

## Rule sketch

In web-spa, a file under `features/<x>/` must not reference a namespace owned by
`features/<y>/` (x != y). Shared UI belongs in `components/` (see the TableHeader/Cell
promotion, commit a2ece8ca); shared state/contracts belong in foundation or web-contracts.
Deliberate exceptions (e.g. the Style Guide page demonstrating other features) get an opt-out
attribute or config, with a reason — mirror the `[ClientOnlyContract(reason)]` pattern.


## Implementation Plan (2026-07-14)

TWA0009 in timewarp-architecture-convention-analyzers.

1. CompilationStart: map namespace → owning feature from declaration file paths. A namespace is
   feature-owned ONLY if every declaration of it in the compilation lives under a single
   `features/<x>/` folder — shared namespaces (Pages, Components, Features.Profiles which
   authentication also declares) are auto-excluded. Bail fast when no tree lives under features/.
2. Per-file semantic walk: identifiers in a features/<x>/ file resolving to a namespace owned by
   feature y != x are flagged, unless a containing type carries [CrossFeatureReference(reason)]
   (matched by name; declared in foundation-contracts/base, mirroring ClientOnlyContract).
3. Scope: .cs only — razor-generated trees are excluded by GeneratedCodeAnalysisFlags.None (the
   markup side is covered indirectly: markup references compile into generated code referencing
   the same namespaces... NOT scanned; documented limitation).
4. Existing coupling triage at first build: StyleGuidePage (deliberate demo aggregation) gets the
   opt-out; anything else assessed case by case.
5. Tests: two fake features via source file paths; cross-ref flags; same-feature clean;
   shell→feature clean; shared-namespace clean; opt-out clean.

## Checklist

- [x] Decide namespace-ownership detection (folder-to-namespace convention already exists)
- [x] Implement in timewarp-architecture-convention-analyzers, next free TWA id
- [x] Opt-out with mandatory reason for deliberate cross-feature demos (Style Guide)
- [x] Tests: cross-feature ref flags; components/ ref clean; opt-out clean
- [x] AnalyzerReleases.Unshipped.md; dev build 0/0

## Session

- Created: 2026-07-11 (spun out of 071 value assessment)

## Results (2026-07-14)

**Implemented** (commit 28b75b2e): `FeatureIsolationAnalyzer` (TWA0009).

- Ownership is derived, not configured: a namespace is feature-owned only when every HAND-WRITTEN
  declaration lives under one `features/<x>/` folder. Shared namespaces (Pages, Components, the
  authentication/profiles overlap) and the `features/base` substrate drop out automatically.
- Hard-won discriminators (found by live debugging, now encoded in tests):
  - Generator trees must be excluded by *.g.cs naming, NOT path-rootedness — the repo persists
    generator output to `artifacts/generated/` (rooted!), and TimeWarp.State's ActionSet partials
    were poisoning `Features.Counters` into multi-owner, silencing the rule.
  - Metadata symbols are exempt: contracts are sharing-by-design (authorization calling the
    GetCurrentUser contract must not flag). Namespace symbols carry no signal (merged).
- `[CrossFeatureReference(reason)]` (foundation-contracts) is the reasoned opt-out.
- **Real drift caught on first sweep**: `UserIds` lived in web-contracts `features/admin` under
  namespace `TimeWarp.Architecture.Services` and was consumed by the authentication feature —
  promoted to `web-contracts/types` as `TimeWarp.Architecture.Types.UserIds`.
- Three deliberate couplings got reasoned opt-outs: StyleGuidePage (demos exercise other
  features' pipelines), CounterPage (app-level store reset), the claims factory (auth/authz pair).
- Documented limits: razor markup (generated trees) not scanned; the convention-analyzers
  project cannot analyze itself.

**Tests**: 6 green (cross-feature flags both identifiers; same-feature, shell→feature,
shared-namespace, opt-out, base-substrate clean). `dev build` 0/0 repo-wide.
