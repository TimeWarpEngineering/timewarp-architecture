# Split TimeWarp.Architecture.Analyzers: move generic code-style rules to TimeWarp.SourceGenerators

## Goal (implementer restatement)

Every `TWA*` id this repo reports is classified. Generic rules check C# file or name shape and do not mention an Architecture concept. That set is TWA0004 (purpose region), TWA0023 (type-stem identifier), and TWA0038 (file length). `timewarp-source-generators` task 034 ports those three, keeping the same ids so `.editorconfig` keys still match. Every other reported id stays in `TimeWarp.Architecture.Analyzers`. Generator diagnostics TWA0001 and TWA0017–TWA0019 stay in `TimeWarp.Architecture.Generators`.

This package keeps emitting TWA0004, TWA0023, and TWA0038 until a `TimeWarp.SourceGenerators` release reports them. `timewarp-state` still takes this package for TWA0004. Deleting the analyzers first would drop that check, and referencing both copies would report each hit twice. The deletion and the SourceGenerators pin bump are one later change on this id.

Acceptance for the classification: the table below marks each id generic, Architecture, retired, or generators-package; the three undecided rows (TWA0023, TWA0028, TWA0036) have a reason; release notes and `documentation/developer/guides/analyzer-package-split.md` name the move set; the three analyzer source files are still in this package.

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
- [ ] Move generic analyzers out; keep Architecture-specific ones here
- [x] Preserve IDs or ship aliases / deprecation period for `.editorconfig` (IDs unchanged; no alias while both packages would report `TWA*`)
- [x] Update AnalyzerReleases / docs / release notes
- [ ] Confirm no non-Architecture consumer still needs this package for style-only rules
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
  The package reports TWA0004, TWA0023, and TWA0038. Bump the pin from 1.0.0-beta.11 and delete
  those three analyzers and their tests from TimeWarp.Architecture.Analyzers in one change.

## Notes

Filed by Jenzi after Steven approved "file the tasks to make this happen" (2026-11-09). Trigger:
Core repo dependency tree showed a false Architecture ↔ State cycle because State references
`TimeWarp.Architecture.Analyzers` only for TWA0004.

Suggested home for generic rules: `timewarp-source-generators` (already ships TW0007; already
referenced by all 15 core repos).

Move set for 034: **TWA0004, TWA0023, TWA0038**. File list and the deletion gate are in
`documentation/developer/guides/analyzer-package-split.md`.

## Results

Classified every `TWA*` id under `source/analyzers/`. The generic move set is TWA0004, TWA0023, and TWA0038. TWA0023 enforces the type-stem naming rule and does not mention an Architecture type; its opt-out attribute stays in `TimeWarp.Architecture.Attributes`. TWA0028 stays Architecture because it checks `TimeWarp.Foundation.Enumerations.Enumeration`. TWA0036 stays Architecture because it is the ISO 8601 rendering policy, including Razor content.

The three generic analyzers are still compiled into `TimeWarp.Architecture.Analyzers`. Deleting them waits on task 034 publishing the same ids. `timewarp-state` is the only `PackageReference` to this package found outside this repo, and it is there for TWA0004 (pin `2.0.0-beta.31`).

Files:

- `documentation/developer/guides/analyzer-package-split.md` — spec for 034 and 153
- `source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md` — release note and row marks
- `source/analyzers/timewarp-architecture-analyzers/AnalyzerReleases.Unshipped.md` — generators ids are out of the split
- `source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj` — package description
- `AGENTS.md` — pointer at the guide

No analyzer behavior changed. Analyzer tests were not re-run. `ganda repo audit` exited 0 with the two advisory warnings already recorded in Session (`kanban-landed` dead claims, `source-file-line-cap`). Neither is auto-fixable from this task.

### How to validate

**Smoke**

```bash
grep -n '| generic |' kanban/in-progress/486-split-timewarparchitectureanalyzers-move-generic-code-style-rules-to-timewarpsourcegenerators/task.md
test -f source/analyzers/timewarp-architecture-convention-analyzers/purpose-region-analyzer.cs
test -f source/analyzers/timewarp-architecture-convention-analyzers/type-stem-identifier-analyzer.cs
test -f source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs
grep -n 'TWA0004, TWA0023, and TWA0038' source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md documentation/developer/guides/analyzer-package-split.md
```

**Expect**

- The classification table has exactly three `| generic |` rows: TWA0004, TWA0023, TWA0038.
- TWA0028 and TWA0036 rows say `Architecture`.
- The three analyzer source files are present.
- Both the unshipped release file and the guide name `TWA0004, TWA0023, and TWA0038`.

**Not in scope:** porting the analyzers into `timewarp-source-generators`, deleting them here, and dropping the State package reference. Those wait on task 034's release, then a resume of this walk.

## Blocked

Cleared 2026-11-10. TimeWarp.SourceGenerators 1.0.0-beta.13 is on nuget.org and reports TWA0004, TWA0023, and TWA0038. Remaining work: bump the pin from 1.0.0-beta.11 and delete those three analyzers and their tests from TimeWarp.Architecture.Analyzers in one change.
