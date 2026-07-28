# Round 3 — independent cross-vendor verification (Claude reviewing Grok's implementation)
**Date:** 2026-07-26
**Scope:** commits 70a45d80 (implement) + 692ed8b0 (review fixes) vs task spec and review
record; full gate re-run; live re-proof of the literal scan. Maintainer-requested.

## Verdict

**Implementation confirmed sound — no bugs.** Every claim verified against code, the dotnet-new
engine's actual generated output, and a live adversarial test of the new gate. Disposition
`clean` stands. Two new non-blocking findings recorded below.

## Highlights

- **Symbol removal complete**: template.json declares only the five architecture flags; platform
  trees merged into the pre-existing unconditional exclude block; AGENTS.md and
  HowToUpgradeToAnalyzerPackages.md updated; only intentional references remain (the smoke
  command's `RemovedTemplateSymbols` absence-assert).
- **`<!--#if (false) -->` slnx technique proven with hard evidence**: the monorepo solution
  keeps the platform projects (round-1 correctly caught the original full deletion; restored in
  692ed8b0), and the actual generated `SmokeDefault` slnx on disk has zero platform entries —
  generated `source/` contains only `Directory.Build.props` + `container-apps`. TWA0008/TWA0010
  are C# syntax-tree analyzers and structurally cannot object; literal boolean conditions were
  already proven by the pre-existing `(true)` exclude block.
- **Literal scan verified AND re-proved live**: scans `.cs` (plus csproj/props/targets/slnx/
  json/razor/proto) — independent of the legacy assert that excludes `.cs`; scoped roots include
  the review-added `source/` + `tests/` Directory.Build.props; the composed-property file is
  structurally exempt (it never contains the continuous literal — no skip-list needed). Planting
  the exact historical a251980f literal (`using TimeWarp.Architecture.TypedIds.Ef;` in
  `postgres-db-context.cs`) failed `template-smoke` immediately at the pre-scan, before any
  packing; reverted clean.
- **Monorepo dogfood + CPM pins untouched** (empty diffs on the auto-detect switches; pins
  value-identical, comment-only edit).
- **Review record honest**: 3 round-1 findings all verifiably fixed in 692ed8b0; round-2
  re-verified; effort disclosed; no swept-under wontfixes.
- **`./bin/dev` residual correctly scoped**: CI invokes the runfile path
  (`.github/workflows/template-smoke.yml:54`), so the stale-AOT footgun is local-dev only.

## Gates (independent re-run, this session)

`dev build` 0/0 · `dev test` all projects 0 failed (counts identical to pre-126-004 baseline) ·
`dev template-smoke` SmokeDefault OK + SmokeNoPostgres OK with the new scan in the path.

## New findings (non-blocking)

- **Suggestion — regex list is convention-by-memory**: `UnsafePlatformNamespaceLiteral`
  (template-smoke-command.cs:93-96) hand-maintains the closed set
  `(Analyzers|Generators|Attributes|TypedIds)`. Verified complete against today's actual
  namespaces, but a future platform sub-namespace added to
  `msbuild/timewarp-platform-packages.props` without a matching regex edit silently escapes the
  scan. Candidate for the standing "prefer analyzers/sourcegen over agreement-by-memory"
  directive: derive the set from the composed-property SSOT or add a guard that cross-checks.
  Maintainer to decide whether to file.
- **Nit — evidence-by-assertion**: the "generated app has no vendored platform trees" gate is
  asserted narratively in the record; hard evidence had to be recovered from leftover
  `artifacts/template-smoke/` output. Future dispositions should paste a short generated-tree
  excerpt.

## Correction (2026-07-27)

The "Gates (independent re-run)" section above overstated one item: the orchestrator's
`dev template-smoke` invocation resolved to the stale Jul-24 AOT `./bin/dev` binary — the exact
footgun this task's own residual warned about — so that particular smoke run predated the
literal scan and did not exercise it (its pass covers the pack/generate asserts only). The scan
itself remains fully proven: the round-3 verifier's planted-literal re-proof ran via the current
runfile path (`dotnet run tools/dev-cli/dev.cs -- template-smoke`) and failed at the pre-scan as
designed. A corrected runfile-path smoke re-run was performed 2026-07-27 during the 126-006
verification (see that task's record). Verdict unchanged; recorded for audit honesty.
