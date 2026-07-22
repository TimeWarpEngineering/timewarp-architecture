# Round 2 — independent orchestrator verification (Claude reviewing Grok's 115)

Method: empirical re-verification of every claim.

## Verified (all pass)

- **Composed package IDs**: `msbuild/timewarp-platform-packages.props` splits the vendor
  fragment from `.Architecture.*` so no continuous sourceName literal exists; BOTH halves of the
  bug are fixed (PackageReference consumers AND the CPM PackageVersion entries at
  Directory.Packages.props:33-35 use the composed properties). template.json has no extra
  replacements; bare "TimeWarp" is not a sourceName form.
- **THE ACID TEST — `template-smoke` run by reviewer via the exact CI invocation**
  (`dotnet run tools/dev-cli/dev.cs -- template-smoke`): SmokeDefault + SmokeNoPostgres both
  generate, restore against the locally-packed feed, and **build 0/0 including test projects**.
  The 54-NU1101 breakage is dead.
- Monorepo `dev build` 0/0 (re-run post-changes).
- **Membership-targets template-safe fix is real**: the engine mangles `'@(...)' != ''`
  compares; replaced with a Count()-based property gate, WITH an explanatory comment.
- identityPackages dual-mode symbol present with source-exclusion conditions (default false =
  identity ships as source until TimeWarp.Identity publishes).
- CI workflow `template-smoke.yml` invokes the same gate — the "this class of break can't ship
  silently" requirement is met.

## Observations (nit-level, no fix required now)

1. `dev template-smoke` fails as "Unknown command" on machines with a stale ganda runfile cache
   (needs `ganda runfile cache --clear`) — cosmetic; CI uses `dotnet run` directly and is
   unaffected. Known runfile-cache gotcha, not a 115 defect.
2. Vendored-source mode (`analyzerPackages=false`) still ships the literal PackageId inside
   timewarp-architecture-analyzers.csproj, which sourceName rewrites to `<App>.Analyzers` — in
   that mode it's the app's own vendored project id, so likely benign; would only matter if a
   vendored app later flips to packages. Edge-of-edge; recorded for awareness.
3. Ops residuals correctly declared by implementer (republish Foundation/Attributes; first
   publish of TimeWarp.Identity, then flip identityPackages default).

## Verdict

Round-1 `clean` CONFIRMED. Claims accurate (one cosmetic command-invocation caveat). The
template's front door works again, and the gate that keeps it working is in CI.
