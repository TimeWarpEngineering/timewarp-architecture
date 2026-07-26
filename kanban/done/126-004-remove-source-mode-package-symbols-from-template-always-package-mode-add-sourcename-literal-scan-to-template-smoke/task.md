# Remove source-mode package symbols from template (always package-mode); add sourceName-literal scan to template-smoke

## Description

Maintainer decision on task 126 (2026-07-26): drop **all three** source-mode template symbols —
`foundationPackages`, `analyzerPackages`, `identityPackages`. Generated apps are **always
package-mode**. The vendored-source modes were the migration-era fallback (pre-publish of the
platform packages, expired since task 124/beta.6+) plus an "eject" story the maintainer
explicitly rejects: the repo is public + Unlicense, so eject = clone; modification happens in
the monorepo (which uses MSBuild source-tree auto-detection, not these symbols) and reaches
generated apps via published packages.

Why removal is a win, not just cleanup:

- The vendored modes ship **untested** — the smoke matrix (`SmokeDefault`/`SmokeNoPostgres`)
  only exercises default flags; `*Packages=false` output has no gate.
- Vendored trees are a fork by construction (sourceName rewrites them into the app);
  `HowToUpgradeToAnalyzerPackages.md` exists to migrate apps *out* of that state.
- They are the largest source of conditional template surface: slnx `#if` nest, template.json
  conditional excludes, TWA0010-class hazards.
- Template-API break (`--analyzerPackages false` stops existing) accepted: template is beta;
  known user base is the maintainer.

**Second half (from 126 RFC Decision 4, unanimous ballot, simplified by this removal):** add a
source-side scan to `dev template-smoke` that fails when template-shipped content contains a raw
continuous platform-namespace literal (`TimeWarp.Architecture…` etc.) outside the
composed-property mechanism (`msbuild/timewarp-platform-packages.props`). The bug class shipped
twice (task 115 origin; repeat caught only by human review in task 113 round-2, commit
`a251980f`). With source-mode content gone, the scan is a simple unconditional check.

## Checklist

### Symbol removal

- [x] `.template.config/template.json`: delete the three symbols, their conditional exclude
      modifiers, and any other conditions referencing them; make the vendored trees
      (`source/foundation/**`, `source/libraries/timewarp-modules/**`, `source/analyzers/**`,
      `source/libraries/timewarp-identity/**`, and their `tests/**` counterparts)
      **unconditionally excluded** from template output
- [x] `timewarp-architecture.slnx`: remove the `<!--#if (!foundationPackages) -->` /
      `(!identityPackages)` / `(!analyzerPackages)` conditional blocks (keep package-mode
      branches as the only content)
- [x] Sweep for other references: csproj/props conditionals keyed on the *template* symbols
      (the MSBuild `UseFoundationPackages`/`UseAnalyzerPackages`/`UseIdentityPackages`
      source-tree auto-detection switches STAY — monorepo dogfooding depends on them),
      TWA0010-relevant `#if` directives, docs
- [x] Update AGENTS.md platform-packages section (remove template-symbol language, e.g.
      "identityPackages=false still vendors…"); keep `HowToUpgradeToAnalyzerPackages.md` as
      the migration doc for pre-existing vendored apps
- [x] Check `dev template-smoke` and `.github/workflows/template-smoke.yml` for any symbol
      plumbing to delete

### sourceName-literal scan (RFC D4)

- [x] Add scan to `dev template-smoke`: packed template content, **including `.cs` files** (the
      existing `AssertPackageIdsNotRewritten` helper filters to
      `.props/.csproj/.targets/.slnx/.json` and excludes `.cs` — the historical bug
      (`using TimeWarp.Architecture.TypedIds.Ef;` in `postgres-db-context.cs`) was in a `.cs`
      file; write a new pass, do not naively reuse the helper)
- [x] Allowlist/route legitimate occurrences through the composed properties; with source-mode
      trees now unconditionally excluded from template output, no `source/analyzers/**` scoping
      gymnastics should be needed — assert that's true
- [x] Prove the scan catches the historical case: temporarily reintroduce the `a251980f`-class
      literal locally and confirm the gate fails

### Verify

- [x] `dev build` 0/0, `dev test`, `dev template-smoke` both matrices green
- [x] Generated-app spot check: `dotnet new` output contains no vendored platform source and no
      dangling references to removed symbols

## Notes

- Parent: 126. Lineage: RFC Decision 4 (unanimous B — smoke-scan over analyzer) folded into
  this task after the maintainer's broader symbol-removal decision superseded its scoping
  concerns; adversarial reviewer's two implementation risks are baked into the checklist above.
- All three symbols verified structurally identical before the decision (template.json sources
  modifiers + slnx conditionals + defaults true).
- Do not touch CPM pins, package IDs, or the composed-property mechanism itself.

### Implementation Plan

#### Goal

Always package-mode for generated apps; drop `foundationPackages` / `analyzerPackages` /
`identityPackages`; keep monorepo `Use*Packages` auto-detect; add sourceName-literal scan
including `.cs` files.

#### Phase 0 — slnx dual-use

Root slnx is monorepo + template. Template conditionals are comments monorepo-side. Removing
source-mode `Project` blocks removes them from monorepo solution membership too (`dev test`
globs `tests/`; pack lists explicit). If monorepo breaks, fallback: `#if (false)` wrap only —
do **not** reintroduce symbols.

#### Phase 1 — template.json + slnx

- `template.json`: delete the 3 symbols + 3 conditional exclude modifiers; merge platform trees
  into an unconditional (`true`) exclude
- `slnx`: remove foundation / libraries / analyzers + their test project regions under
  `*Packages` conditionals; keep `api` / `web` / `grpc` / `yarp` / `postgres` flags
- Do **not** change `Use*Packages` MSBuild, composed props, or CPM

#### Phase 2 — doc/comment sweep

- `AGENTS.md` platform section
- `HowToUpgradeToAnalyzerPackages.md`
- Comments in: `Directory.Packages.props`, web-contracts, timewarp-testing, identity-tests,
  web-infrastructure-tests, missing-invariants-validator-exception, template-smoke-command
  Design region
- Leave: kanban history, dual-mode code, TWA0010
- CI `template-smoke.yml` likely no change

#### Phase 3 — scan (new pass, not `AssertPackageIdsNotRewritten`)

`AssertNoUnsafePlatformNamespaceLiterals` on template-shipped consumer content (`.cs` included):

- Regex: `TimeWarp\.Architecture\.(Analyzers|Generators|Attributes|TypedIds)\b`
- Roots: `source/container-apps`, `tests/common`, `tests/container-apps`, `msbuild`, root
  props / slnx / `global.json` etc.
- Skip: foundation / analyzers / libraries platform trees, docs, kanban, tools
- Post-generate: assert vendored trees absent; symbols gone from generated `template.json`
- Optional: assert rewritten `{appName}.TypedIds` etc. in generated tree including `.cs`
- Prove: temporary `using TimeWarp.Architecture.TypedIds.Ef;` in `postgres-db-context.cs` must
  fail smoke; then revert

#### Verify

- `dev build` 0/0, `dev test`, `template-smoke` both matrices
- Generated-app spot check
- No `--*Packages` on help

#### Out of scope

CPM pins, package IDs, `timewarp-platform-packages.props` composition values.

## Session

- Created: 2026-07-26 — filed from 126 maintainer decision (drop all three symbols) + RFC D4.
- Planning: 2026-07-26
- Implementer: grok session 2026-07-26
- Review: grok Phase 4b effort 1 (round 1 + round 2) 2026-07-26
- Orchestration complete: 2026-07-26

## Results

### What was implemented
1. **Always package-mode template** — deleted `foundationPackages` / `analyzerPackages` / `identityPackages` from `.template.config/template.json` and merged platform trees into the unconditional `(true)` exclude.
2. **Dual-use slnx** — monorepo keeps platform + platform-test projects under `<!--#if (false) -->` (always stripped on generate; no template symbols). Review fix restored membership after initial full deletion.
3. **Monorepo dual-mode retained** — `UseFoundationPackages` / `UseAnalyzerPackages` / `UseIdentityPackages` auto-detect + ProjectReference/PackageReference paths untouched; CPM pins and composed props unchanged.
4. **Docs/comments** — AGENTS.md, HowToUpgradeToAnalyzerPackages.md, and dual-mode comments updated for always package-mode greenfield.
5. **sourceName-literal scan (RFC D4)** — new independent smoke passes:
   - `AssertNoUnsafePlatformNamespaceLiterals` (includes `.cs`; regex `TimeWarp.Architecture.(Analyzers|Generators|Attributes|TypedIds)`)
   - `AssertRemovedPackageSymbolsGoneFromTemplateConfig`
   - `AssertGeneratedAppPackageMode` (vendored trees + rewritten `{appName}.*` belt)
6. **Prove-scan** — temporary `using TimeWarp.Architecture.TypedIds.Ef;` in `postgres-db-context.cs` failed smoke; poison reverted (not committed).

### Files changed (primary)
- `.template.config/template.json`
- `timewarp-architecture.slnx`
- `tools/dev-cli/endpoints/template-smoke-command.cs`
- `AGENTS.md`, `documentation/developer/how-to-guides/HowToUpgradeToAnalyzerPackages.md`
- Comment touch-ups: Directory.Packages.props, web-contracts, timewarp-testing, identity/infrastructure tests, missing-invariants-validator-exception

### Key decisions / deviations
- **`#if (false)` dual-use** preferred over permanent monorepo solution exclusion (plan Phase 0 fallback; review M1).
- Scan roots extended with `source/Directory.Build.props` + `tests/Directory.Build.props` (review M3).
- Template API break (`--*Packages false` gone) accepted (beta; maintainer decision).

### Verification
| Gate | Result |
|------|--------|
| `dev build` / monorepo slnx build | **0/0** |
| Platform tests via ProjectReference + globs | pass (implementer) |
| `dotnet run tools/dev-cli/dev.cs -- template-smoke` | **SUCCEEDED** (SmokeDefault + SmokeNoPostgres) after review fixes |
| Prove a251980f class | smoke **failed** on TypedIds using; reverted |
| Generated SmokeDefault | no vendored platform trees; no `*Packages` template options |

### Phase 4b review
- **Effort:** 1 (general only)
- **Rounds:** 2
- **Final counts:** suggestion 2 fixed, nit 1 fixed, **0 open**
- **Disposition:** `clean` — `review/disposition.md`
- **Paths:** `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/round-2/{general,merged}.md`, `review/disposition.md`

### Residual risks
- Prefer `dotnet run tools/dev-cli/dev.cs -- template-smoke` over stale AOT `./bin/dev` until re-self-install.
- Dual-mode `Use*Packages` branches remain in shipping csproj for monorepo dogfood only; generated apps always take the package branch.
