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

TWPA0009 in timewarp-architecture-convention-analyzers.

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

- [ ] Decide namespace-ownership detection (folder-to-namespace convention already exists)
- [ ] Implement in timewarp-architecture-convention-analyzers, next free TWPA id
- [ ] Opt-out with mandatory reason for deliberate cross-feature demos (Style Guide)
- [ ] Tests: cross-feature ref flags; components/ ref clean; opt-out clean
- [ ] AnalyzerReleases.Unshipped.md; dev build 0/0

## Session

- Created: 2026-07-11 (spun out of 071 value assessment)
