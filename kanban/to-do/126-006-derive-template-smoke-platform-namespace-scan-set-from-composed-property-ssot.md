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

## Session

- Created: 2026-07-26 — filed from 126-004 round-3 verification finding per maintainer request.
