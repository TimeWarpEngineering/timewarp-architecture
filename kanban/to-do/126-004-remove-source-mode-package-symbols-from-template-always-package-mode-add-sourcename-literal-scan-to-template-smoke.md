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

- [ ] `.template.config/template.json`: delete the three symbols, their conditional exclude
      modifiers, and any other conditions referencing them; make the vendored trees
      (`source/foundation/**`, `source/libraries/timewarp-modules/**`, `source/analyzers/**`,
      `source/libraries/timewarp-identity/**`, and their `tests/**` counterparts)
      **unconditionally excluded** from template output
- [ ] `timewarp-architecture.slnx`: remove the `<!--#if (!foundationPackages) -->` /
      `(!identityPackages)` / `(!analyzerPackages)` conditional blocks (keep package-mode
      branches as the only content)
- [ ] Sweep for other references: csproj/props conditionals keyed on the *template* symbols
      (the MSBuild `UseFoundationPackages`/`UseAnalyzerPackages`/`UseIdentityPackages`
      source-tree auto-detection switches STAY — monorepo dogfooding depends on them),
      TWA0010-relevant `#if` directives, docs
- [ ] Update AGENTS.md platform-packages section (remove template-symbol language, e.g.
      "identityPackages=false still vendors…"); keep `HowToUpgradeToAnalyzerPackages.md` as
      the migration doc for pre-existing vendored apps
- [ ] Check `dev template-smoke` and `.github/workflows/template-smoke.yml` for any symbol
      plumbing to delete

### sourceName-literal scan (RFC D4)

- [ ] Add scan to `dev template-smoke`: packed template content, **including `.cs` files** (the
      existing `AssertPackageIdsNotRewritten` helper filters to
      `.props/.csproj/.targets/.slnx/.json` and excludes `.cs` — the historical bug
      (`using TimeWarp.Architecture.TypedIds.Ef;` in `postgres-db-context.cs`) was in a `.cs`
      file; write a new pass, do not naively reuse the helper)
- [ ] Allowlist/route legitimate occurrences through the composed properties; with source-mode
      trees now unconditionally excluded from template output, no `source/analyzers/**` scoping
      gymnastics should be needed — assert that's true
- [ ] Prove the scan catches the historical case: temporarily reintroduce the `a251980f`-class
      literal locally and confirm the gate fails

### Verify

- [ ] `dev build` 0/0, `dev test`, `dev template-smoke` both matrices green
- [ ] Generated-app spot check: `dotnet new` output contains no vendored platform source and no
      dangling references to removed symbols

## Notes

- Parent: 126. Lineage: RFC Decision 4 (unanimous B — smoke-scan over analyzer) folded into
  this task after the maintainer's broader symbol-removal decision superseded its scoping
  concerns; adversarial reviewer's two implementation risks are baked into the checklist above.
- All three symbols verified structurally identical before the decision (template.json sources
  modifiers + slnx conditionals + defaults true).
- Do not touch CPM pins, package IDs, or the composed-property mechanism itself.

## Session

- Created: 2026-07-26 — filed from 126 maintainer decision (drop all three symbols) + RFC D4.
