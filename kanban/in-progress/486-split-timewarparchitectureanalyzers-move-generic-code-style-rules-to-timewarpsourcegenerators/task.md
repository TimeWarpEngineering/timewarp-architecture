# Split TimeWarp.Architecture.Analyzers: move generic code-style rules to TimeWarp.SourceGenerators

## Goal (implementer restatement)

`TimeWarp.SourceGenerators` 1.0.0-beta.13 reports TWA0004, TWA0023, and TWA0038 with the same ids and the same defaults (purpose region and file length warn and are enabled; type-stem naming warns and is off). This repo already references that package. Leaving the three analyzers in `TimeWarp.Architecture.Analyzers` would report each hit twice, so the pin bump and the deletion are one change.

Every other reported id stays in this package. TWA0028 and TWA0036 stay Architecture. Generator diagnostics TWA0001 and TWA0017–TWA0019 stay in `TimeWarp.Architecture.Generators`. The TWA0023 opt-out attribute stays in `TimeWarp.Architecture.Attributes`. `.editorconfig` keys are unchanged because the ids do not change.

Acceptance: `Directory.Packages.props` pins `TimeWarp.SourceGenerators` at 1.0.0-beta.13; the three analyzer sources and their tests are gone from `timewarp-architecture-convention-analyzers`; the unshipped release table no longer lists those ids; the package description and `documentation/developer/guides/analyzer-package-split.md` say SourceGenerators reports them; the build still enforces TWA0004 and TWA0038 through that package. `timewarp-state` is the only external reference (published pin `2.0.0-beta.31`, which still contains the rules). Dropping that reference is task 153.

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

- `timewarp-source-generators` **034** — Receive generic TWA rules (TWA0004, TWA0023, TWA0038) from
  TimeWarp.Architecture.Analyzers. The classification table below is the move set. Port those ids
  unchanged. Do not port any row whose class is not `generic`.
- `timewarp-state` **153** — Drop TimeWarp.Architecture.Analyzers; get TWA0004 from
  TimeWarp.SourceGenerators (blocked by 034's release). State sets `TWA0004` to error and the other
  listed `TWA*` severities to none. It has no `TWA0038` entry, so an enabled-by-default TWA0038
  inside SourceGenerators is a new warning there.

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

## Classification

Package: `TimeWarp.Architecture.Analyzers` ← `timewarp-architecture-convention-analyzers`.

Class `generic` is the move set for task 034. Class `Architecture` stays in this package.
Class `retired` is reserved and is not reported. Class `generators-package` is
`timewarp-architecture-analyzers` (`TimeWarp.Architecture.Generators`) and is not this split.

| ID | Analyzer | Class | Notes |
|----|----------|-------|-------|
| TWA0001 | partial-class-declaration | generators-package | Do not move |
| TWA0002 | contract-nullability-validator | Architecture | Nullable property with a presence rule |
| TWA0003 | contract-nullability-validator | Architecture | Required property with an empty-string default |
| TWA0004 | purpose-region | generic | `#region Purpose`. Warning, enabled. State's reason for the reference |
| TWA0005 | endpoint-coverage | retired | Constant only. Not in `SupportedDiagnostics`. Do not reuse |
| TWA0006 | endpoint-coverage | Architecture | Routed contract has no server endpoint |
| TWA0007 | aspire-resource-name | Architecture | Aspire `ServiceNames` |
| TWA0008 | template-conditional-token | Architecture | Template conditional tokens |
| TWA0009 | slice-isolation | Architecture | Cross-slice references |
| TWA0010 | template-flag-constants | Architecture | Template flag missing from `DefineConstants` |
| TWA0011 | aggregate-invariants | Architecture | Missing nested `Invariants` validator |
| TWA0012 | aggregate-invariants | Architecture | `Invariants` validator is not private |
| TWA0013 | endpoint-auth-posture | Architecture | Missing auth marker |
| TWA0014 | endpoint-auth-posture | Architecture | Contradictory auth posture |
| TWA0015 | feature-filename-grammar | Architecture | Function segment pairs with the wrong layer |
| TWA0016 | feature-filename-grammar | Architecture | Unregistered function segment |
| TWA0017 | ingress-route-prefix | generators-package | Do not move |
| TWA0018 | ingress-route-prefix | generators-package | Do not move |
| TWA0019 | ingress-route-prefix | generators-package | Do not move |
| TWA0020 | endpoint-auth-posture | Architecture | `[ApiEndpoint]` with `[ClientOnlyContract]` |
| TWA0021 | mock-authentication-registration | Architecture | Mock auth registration site |
| TWA0022 | spa-mediator-send | Architecture | Direct mediator `Send` in the SPA |
| TWA0023 | type-stem-identifier | generic | Naming convention (`tw-csharp`). Warning, disabled by default. Opt-out attribute stays in `TimeWarp.Architecture.Attributes` and is matched by simple name |
| TWA0024 | endpoint-authorize-policy-agreement | Architecture | Policy is not registered |
| TWA0025 | page-local-message-bar | Architecture | FluentUI / shell message bars |
| TWA0026 | component-side-effect | Architecture | Component calls a side effect directly |
| TWA0027 | component-side-effect | Architecture | Empty opt-out reason |
| TWA0028 | enumeration-member-shape | Architecture | Confirmed. Checks `TimeWarp.Foundation.Enumerations.Enumeration` members. Not generic style |
| TWA0029 | — | retired | Server-offers. Do not reuse |
| TWA0030 | — | retired | Server-offers. Do not reuse |
| TWA0031 | — | retired | Server-offers. Do not reuse |
| TWA0032 | direct-fluent-text-area | Architecture | FluentUI `TextArea` |
| TWA0033 | referenceable-resource | Architecture | Contract shape |
| TWA0034 | referenceable-resource | Architecture | Duplicate `@` prefix |
| TWA0035 | operation-attribute | Architecture | Missing `[Operation]` |
| TWA0036 | iso-date-rendering | Architecture | Confirmed. ISO 8601 rendering policy, including Razor content. Not generic style |
| TWA0037 | direct-fluent-message-bar | Architecture | FluentUI `MessageBar` |
| TWA0038 | file-length | generic | 500 lines by default. Key `dotnet_diagnostic.TWA0038.max_lines`. Warning, enabled |
| TWA0039 | contract-agreement | Architecture | `I*Details` member agreement |
| TWA0040 | contract-agreement | Architecture | `Set<Name>` mutation |
| TWA0041 | contract-agreement | Architecture | Query projection |
| TWA0042 | contract-agreement | Architecture | Uncovered command |
| TWA0043 | contract-agreement | Architecture | More than one aggregate root |

No `TWA*` id above TWA0043 is declared under `source/analyzers/`.

## Checklist

- [x] Complete the TWA inventory and classification table (generic vs Architecture-specific)
- [x] Agree the move set with Source Generators (task 034) before deleting from this package
- [x] Move generic analyzers out; keep Architecture-specific ones here
- [x] Preserve IDs or ship aliases / deprecation period for `.editorconfig` (IDs unchanged in TimeWarp.SourceGenerators 1.0.0-beta.13; this package no longer reports them)
- [x] Update AnalyzerReleases / docs / release notes
- [x] Confirm no non-Architecture consumer still needs this package for style-only rules (only `timewarp-state`, pin `2.0.0-beta.31`; task 153 drops it)
- [x] Cross-link results back to tasks 034 and 153

## Session

- Created: 1529859 (2026-11-09)
- Audit fixes landed on this branch before the walk: suppression baseline seeded,
  `/.timewarp/signals/` ignored, TimeWarp.Nuru 4.0.0-beta.12 with Amuru 3.0.0-beta.2 and
  Terminal 2.0.0-beta.1. CLI handlers are `IRequestHandler` and return `ValueTask` so
  `tools/dev-cli/dev.cs` and `tools/agent-identity-cli/agent.cs` build. Two advisory
  warnings remain (`kanban-landed` dead claims, `source-file-line-cap`); neither is auto-fixable.
- Implementer: grok task-work (2026-11-10). Classification, package description, and
  `documentation/developer/guides/analyzer-package-split.md`. Analyzers not deleted.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1c16f-8d9b-7231-a926-7580c628763a, max-turns 500, Blocked — 2026-11-09T18:05:29Z
- Resume: TimeWarp.SourceGenerators 1.0.0-beta.13 is on nuget.org (source-generators task 034).
  The package reports TWA0004, TWA0023, and TWA0038.
- Implementer: grok task-work (2026-11-10). Pinned TimeWarp.SourceGenerators at 1.0.0-beta.13 and
  removed TWA0004, TWA0023, and TWA0038 from TimeWarp.Architecture.Analyzers, with their tests.
- Audit boy-scout: `ganda repo audit --fix` lowered `.timewarp/suppression-baseline.json` (the deleted
  type-stem exemptions test no longer suppresses TWA0023) and redeployed `.githooks/*.cs`. Converted
  `.github/workflows/workflow.yml` run steps to pwsh so `workflow-pwsh-shell` passes (the check is
  new with timewarp-ganda task 508; the file had no product diff before this). `source-file-line-cap`
  stays a warning (task 469-001).
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1c4e9-51d9-77c1-a4b8-6234c3bcd8e0, max-turns 500, Done — 2026-11-10T10:46:21Z
- Review oracle round 1 (2026-11-10): claude-opus-5-5, effort 3, roster general. Found 0 bugs, 1 suggestion,
  and 1 nit, all open. A fix pass for M1 and M2 is requested. See `review/round-1/merged.md`.

## Notes

Filed by Jenzi after Steven approved "file the tasks to make this happen" (2026-11-09). Trigger:
Core repo dependency tree showed a false Architecture ↔ State cycle because State references
`TimeWarp.Architecture.Analyzers` only for TWA0004.

Suggested home for generic rules: `timewarp-source-generators` (already ships TW0007; already
referenced by all 15 core repos).

Move set for 034: **TWA0004, TWA0023, TWA0038**. File list and the deletion gate are in
`documentation/developer/guides/analyzer-package-split.md`.

## Results

`Directory.Packages.props` pins `TimeWarp.SourceGenerators` at 1.0.0-beta.13, which reports TWA0004, TWA0023, and TWA0038. Those three analyzers and their tests are removed from `TimeWarp.Architecture.Analyzers` in the same change, so a build reports each hit once. Architecture rules stay. TWA0001 and TWA0017–TWA0019 stay in `TimeWarp.Architecture.Generators`. `TypeStemIdentifierAttribute` stays in `TimeWarp.Architecture.Attributes`.

TWA0028 stays Architecture (it checks `TimeWarp.Foundation.Enumerations.Enumeration`). TWA0036 stays Architecture (ISO 8601 rendering, including Razor). The only `PackageReference` to `TimeWarp.Architecture.Analyzers` outside this repo is `timewarp-state` at published pin `2.0.0-beta.31`, which still contains the three rules. Dropping that reference is timewarp-state task 153. This repo's unpublished version stays `2.0.0-beta.32`.

`./bin/dev build --quiet` succeeded. Analyzer tests: 263 passed, 0 failed (Jaribu, from the test project directory, Release, `--no-build`). A throwaway 510-line file with no Purpose region, since deleted, produced error TWA0004 and error TWA0038 through `timewarp-source-generators` 1.0.0-beta.13. Each id appeared twice in the log because MSBuild echoes the csc line. The csc command line named that analyzer once.

`ganda repo audit --fix` lowered the suppression baseline and redeployed the git hooks. `.github/workflows/workflow.yml` run steps are pwsh so `workflow-pwsh-shell` passes. `source-file-line-cap` remains a warning for the same generated and research files as before (task 469-001).

### Review

Round 1 (general, effort 3) has 2 open findings and no disposition yet. M1 (suggestion): AGENTS.md and the split guide repeat the exact SourceGenerators pin; write the minimum version and point at `Directory.Packages.props`. M2 (nit): the `TypeStemIdentifierAttribute` Design region still names the convention analyzers as the TWA0023 matcher. The reviewer re-ran the gates and they pass: `dev build` had 0 warnings and 0 errors, and the analyzer tests passed 263 of 263. Paths: `review/review-framework.md` and `review/round-1/merged.md`.

### How to validate

**Smoke**

```bash
grep -n 'TimeWarp.SourceGenerators" Version="1.0.0-beta.13"' Directory.Packages.props
test ! -f source/analyzers/timewarp-architecture-convention-analyzers/purpose-region-analyzer.cs
test ! -f source/analyzers/timewarp-architecture-convention-analyzers/type-stem-identifier-analyzer.cs
test ! -f source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs
test ! -f tests/analyzers/timewarp-architecture-analyzers-tests/purpose-region-analyzer-tests.cs
test ! -f tests/analyzers/timewarp-architecture-analyzers-tests/file-length-analyzer-tests.cs
test ! -f tests/analyzers/timewarp-architecture-analyzers-tests/type-stem-identifier-naming-analyzer-tests.cs
test -f source/analyzers/timewarp-architecture-attributes/type-stem-identifier-attribute.cs
awk -F'|' '/^\| TWA/ && $4 ~ /generic/ { print $2 }' kanban/in-progress/486-split-timewarparchitectureanalyzers-move-generic-code-style-rules-to-timewarpsourcegenerators/task.md
grep -n '1.0.0-beta.13' documentation/developer/guides/analyzer-package-split.md source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md
```

**Expect**

- The pin is `1.0.0-beta.13`.
- The three analyzer sources and their tests are absent. The opt-out attribute file is present.
- The awk lines are TWA0004, TWA0023, and TWA0038. TWA0028 and TWA0036 rows say Architecture.
- The guide and the unshipped release file both mention 1.0.0-beta.13. The unshipped table has no TWA0004, TWA0023, or TWA0038 rows.
- A project that imports the root `Directory.Build.props` still fails the build on a missing Purpose region or a file over 500 lines, from SourceGenerators, once.
