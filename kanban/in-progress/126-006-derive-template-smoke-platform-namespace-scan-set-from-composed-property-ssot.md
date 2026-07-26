# Derive template-smoke platform-namespace scan set from composed-property SSOT

## Description

Close the convention-by-memory gap found by 126-004's round-3 independent verification
(`kanban/done/126-004-…/review/round-3/independent-verification.md`, first new finding).

The sourceName-literal scan added by 126-004 guards template-shipped content against continuous
`TimeWarp.Architecture.*` platform-namespace literals (the a251980f bug class). Its detection
set is a **hand-maintained regex**:

- `tools/dev-cli/endpoints/template-smoke-command.cs:93-96` — `UnsafePlatformNamespaceLiteral`,
  matching `TimeWarp\.Architecture\.(Analyzers|Generators|Attributes|TypedIds)\b`.

That closed set was verified complete against today's actual namespaces, but it duplicates
knowledge whose SSOT is `msbuild/timewarp-platform-packages.props` (the composed
`$(_TwPlatformVendor).Architecture.*` package IDs / namespace properties). If a future platform
sub-namespace is added there without a matching regex edit, the scan silently misses it — the
exact agreement-by-memory pattern the repo's standing directive (AGENTS.md "prefer
analyzers/source generators over convention-by-memory") exists to kill. Two things must agree;
one should be generated from the other or a build-time check must force the agreement.

**Direction (implementer chooses the cleanest, discuss only if neither fits):**

1. **Derive at runtime**: the smoke command parses `msbuild/timewarp-platform-packages.props`
   (it is checked-in XML; simple to read) and builds the alternation from the composed suffixes
   found there, so a new property is picked up automatically. `TypedIds` note: verify whether a
   composed property for it exists post-113 (`TwArchitectureTypedIdsEfNamespace`) — the
   derivation must capture namespace-composition properties, not just PackageId properties.
2. **Cross-check guard**: keep the regex constant but add a startup assertion in the smoke
   command that every `Architecture.*` suffix present in the props file is covered by the
   regex — fails loudly on drift instead of silently narrowing coverage.

Either way, drift becomes impossible without a red gate.

## Checklist

- [ ] Inventory the props file's composed `Architecture.*` suffixes (PackageId properties AND
      namespace properties like the TypedIds Ef using) and confirm the current regex set maps
      1:1 to them
- [ ] Implement derivation (option 1) or cross-check guard (option 2) in
      `template-smoke-command.cs`; keep the scan's file-set and structural exemption
      (composed-property file never contains the continuous literal) unchanged
- [ ] Prove drift detection: locally add a fake `TwArchitectureFakeThingPackageId` property to
      the props file and confirm the smoke command either scans for the new suffix (option 1)
      or fails the cross-check (option 2); revert
- [ ] Re-prove the historical case still fails: plant `using TimeWarp.Architecture.TypedIds.Ef;`
      in `postgres-db-context.cs`, confirm immediate pre-scan failure, revert (pattern from the
      126-004 round-3 verification)
- [ ] Update the scan's Purpose/Design region to state the set is derived/guarded, not
      hand-maintained
- [ ] Gates: `dev build` 0/0, `dev template-smoke` both matrices (dev test not expected to be
      affected — run if any shared code moves)

## Notes

- Parent: 126. Origin: 126-004 round-3 verification suggestion (2026-07-26); maintainer ordered
  filing same day.
- Related records: 126-004 task.md Results addendum; `msbuild/timewarp-platform-packages.props`
  (SSOT); 126 RFC D4 (the scan's ballot lineage).
- Scope guard: this hardens the existing scan only — no new analyzer, no scan-scope expansion,
  no changes to what content is scanned.

### Implementation Plan (2026-07-27)

#### Goal
Eliminate the convention-by-memory gap in `AssertNoUnsafePlatformNamespaceLiterals`: stop
hand-maintaining the closed suffix set `(Analyzers|Generators|Attributes|TypedIds)` and derive
it at runtime from `msbuild/timewarp-platform-packages.props`, so adding a composed
`Architecture.*` property automatically extends scan coverage (or fails the smoke gate loudly
if derivation breaks).

**Scope guard (unchanged):** harden the existing monorepo pre-scan only — no new analyzer, no
scan-root/extension expansion, no change to structural exemption of the props file.

#### Chosen approach: Option 1 — Derive at runtime
Prefer derivation over dual-edit cross-check (option 2). Props file is tiny, stable, checked-in
MSBuild XML. Matches AGENTS.md "prefer generate/check over convention-by-memory".

#### Inventory (props SSOT vs current regex) — 1:1 today
- TwArchitectureAnalyzersPackageId → Analyzers
- TwArchitectureGeneratorsPackageId → Generators
- TwArchitectureAttributesPackageId → Attributes
- TwArchitectureAttributesNamespace → Attributes (dup)
- TwArchitectureTypedIdsEfNamespace → **TypedIds** (namespace-only; must not filter to PackageId-only)
- TwArchitectureRootNamespace → skip (no third segment)

#### Exact files
| File | Change |
|------|--------|
| tools/dev-cli/endpoints/template-smoke-command.cs | Sole production change |
| msbuild/timewarp-platform-packages.props | Temporary fake property for drift proof only, then revert |
| postgres-db-context.cs | Temporary historical plant only, then revert |

#### Parsing strategy
1. Load `Path.Combine(RepoRoot, "msbuild", "timewarp-platform-packages.props")` via XDocument
2. Walk PropertyGroup property element values (not PackageId-only)
3. Capture first segment after `.Architecture.` with regex accepting `$(_TwPlatformVendor).Architecture.X…`
4. Distinct + stable sort; hard-fail if empty
5. Build `TimeWarp\.Architecture\.(…)\b` with Regex.Escape per suffix
6. Not a static field — needs RepoRoot; build once per Handle after FindRepoRoot
7. Log derived suffixes for operator visibility

#### Structural exemption
Unchanged: composed `$(_Tw…)` values never contain continuous literals; do not special-case
props out of scan.

#### Proofs
- Drift: add TwArchitectureFakeThingPackageId → expect FakeThing in derived suffixes; optional
  plant to confirm hit; revert
- Historical: plant `using TimeWarp.Architecture.TypedIds.Ef;` in postgres-db-context.cs →
  pre-scan fail; revert
- Happy: clean tree → template-smoke both matrices OK

#### Gates
dev build 0/0; `dotnet run tools/dev-cli/dev.cs -- template-smoke` both matrices; dev test not
required unless shared code moves

#### Locked decisions
1. Option 1 derive
2. First segment after Architecture (TypedIds from TypedIds.Ef)
3. Value-based extraction (PackageId AND namespace props)
4. Hard fail on empty set
5. No scan surface changes
6. Single production file: template-smoke-command.cs

Out of scope: ForbiddenRewrittenPackageFragments parallel list (optional follow-up)

## Session

- Created: 2026-07-26 — filed from 126-004 round-3 verification finding per maintainer request.
- Orchestration plan: grok (2026-07-27)
