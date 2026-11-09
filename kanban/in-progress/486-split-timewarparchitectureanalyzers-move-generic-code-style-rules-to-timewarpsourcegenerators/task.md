# Split TimeWarp.Architecture.Analyzers: move generic code-style rules to TimeWarp.SourceGenerators

## Description

`TimeWarp.Architecture.Analyzers` (package id from `TwArchitectureAnalyzersPackageId`, project
`source/analyzers/timewarp-architecture-convention-analyzers`) currently mixes generic C# code-style
rules with Architecture-specific conventions (contracts, endpoints, aggregates, slices, Aspire,
FluentUI page rules).

Non-Architecture repos such as `timewarp-state` reference this package only for generic rules
(State: `Directory.Build.props` line 47, `PrivateAssets="all"`, comment names TWA0004). That
reference shows up as an Architecture ↔ State edge in the Core repo dependency tree even though
State never uses Architecture library code, and Architecture already depends on State as a library.

`TimeWarp.SourceGenerators` already ships generic style rules (for example TW0007) and is already
referenced by all core repos. Generic TWA rules belong there. Architecture-specific rules stay in
`TimeWarp.Architecture.Analyzers`.

Decision context (Steven, 2026-11-09, Jenzi chat): count analyzers that generate code as dependency
edges; ignore analyzers that only check style. Split by what each rule checks. Preserve diagnostic
IDs (or aliases / a deprecation period) so consumers' `.editorconfig` does not break.

## Related tasks

- `timewarp-source-generators` **034** — Receive generic TWA rules (TWA0004 etc.) from
  TimeWarp.Architecture.Analyzers (blocked by this task's classification).
- `timewarp-state` **153** — Drop TimeWarp.Architecture.Analyzers; get TWA0004 from
  TimeWarp.SourceGenerators (blocked by 034's release).

## Requirements

1. **Inventory and classify every TWA rule** (first deliverable). Fill the classification table
   below for every ID shipped by `TimeWarp.Architecture.Analyzers` /
   `timewarp-architecture-convention-analyzers` (and note any TWA IDs that live only in
   `timewarp-architecture-analyzers` / generators so they are not moved by accident).
2. Move rules classified **generic** into `timewarp-source-generators` (landed by task 034).
3. Keep rules classified **Architecture-specific** in `TimeWarp.Architecture.Analyzers`.
4. Preserve diagnostic IDs, or provide aliases / a deprecation period, so existing
   `.editorconfig` / globalconfig severity settings keep working.
5. Update docs and release notes in this repo for the split and for any package consumers that
   should stop taking Architecture.Analyzers for style-only use.
6. After 034 releases and consumers switch, no non-Architecture repo should need
   `TimeWarp.Architecture.Analyzers` for generic rules.

## Starter inventory (complete and classify in step 1)

Package: `TimeWarp.Architecture.Analyzers` ← `timewarp-architecture-convention-analyzers`.

| ID | Analyzer | Suggested class | Notes |
|----|----------|-----------------|-------|
| TWA0002 | contract-nullability-validator | Architecture | contracts |
| TWA0003 | contract-nullability-validator | Architecture | contracts |
| TWA0004 | purpose-region | **generic (candidate)** | `#region Purpose`; State's only reason for the reference |
| TWA0005 | endpoint-coverage | Architecture | endpoints |
| TWA0006 | endpoint-coverage | Architecture | endpoints |
| TWA0007 | aspire-resource-name | Architecture | Aspire |
| TWA0008 | template-conditional-token | Architecture | template |
| TWA0009 | slice-isolation | Architecture | slices |
| TWA0010 | template-flag-constants | Architecture | template |
| TWA0011 | aggregate-invariants | Architecture | aggregates |
| TWA0012 | aggregate-invariants | Architecture | aggregates |
| TWA0013 | endpoint-auth-posture | Architecture | endpoints |
| TWA0014 | endpoint-auth-posture | Architecture | endpoints |
| TWA0015 | feature-filename-grammar | Architecture | feature grammar |
| TWA0016 | feature-filename-grammar | Architecture | feature grammar |
| TWA0020 | endpoint-auth-posture | Architecture | endpoints |
| TWA0021 | mock-authentication-registration | Architecture | auth |
| TWA0022 | spa-mediator-send | Architecture | SPA / Mediator |
| TWA0023 | type-stem-identifier | ? | confirm: naming convention vs Architecture-only |
| TWA0024 | endpoint-authorize-policy-agreement | Architecture | endpoints |
| TWA0025 | page-local-message-bar | Architecture | FluentUI / pages |
| TWA0026 | component-side-effect | Architecture | components |
| TWA0027 | component-side-effect | Architecture | components |
| TWA0028 | enumeration-member-shape | ? | confirm: domain Enumeration vs generic |
| TWA0032 | direct-fluent-text-area | Architecture | FluentUI |
| TWA0033 | referenceable-resource | Architecture | resource refs |
| TWA0034 | referenceable-resource | Architecture | resource refs |
| TWA0035 | operation-attribute | Architecture | contracts / operations |
| TWA0036 | iso-date-rendering | ? | confirm: UI date rendering vs generic |
| TWA0037 | direct-fluent-message-bar | Architecture | FluentUI |
| TWA0038 | file-length | **generic (candidate)** | file length limit; useful outside Architecture |
| TWA0039–TWA0043 | contract-agreement | Architecture | contracts / aggregates |

Also present under `timewarp-architecture-analyzers` (generators package, different PackageId —
do **not** move as part of the style split without an explicit decision): TWA0001 (partial class),
TWA0017–TWA0019 (ingress route prefix generator diagnostics).

## Checklist

- [ ] Complete the TWA inventory and classification table (generic vs Architecture-specific)
- [ ] Agree the move set with Source Generators (task 034) before deleting from this package
- [ ] Move generic analyzers out; keep Architecture-specific ones here
- [ ] Preserve IDs or ship aliases / deprecation period for `.editorconfig`
- [ ] Update AnalyzerReleases / docs / release notes
- [ ] Confirm no non-Architecture consumer still needs this package for style-only rules
- [ ] Cross-link results back to tasks 034 and 153

## Session

- Created: 1529859 (2026-11-09)
- Audit fixes landed on this branch before the walk: suppression baseline seeded,
  `/.timewarp/signals/` ignored, TimeWarp.Nuru 4.0.0-beta.12 with Amuru 3.0.0-beta.2 and
  Terminal 2.0.0-beta.1. CLI handlers are `IRequestHandler` and return `ValueTask` so
  `tools/dev-cli/dev.cs` and `tools/agent-identity-cli/agent.cs` build. Two advisory
  warnings remain (`kanban-landed` dead claims, `source-file-line-cap`); neither is auto-fixable.

## Notes

Filed by Jenzi after Steven approved "file the tasks to make this happen" (2026-11-09). Trigger:
Core repo dependency tree showed a false Architecture ↔ State cycle because State references
`TimeWarp.Architecture.Analyzers` only for TWA0004.

Suggested home for generic rules: `timewarp-source-generators` (already ships TW0007; already
referenced by all 15 core repos).
