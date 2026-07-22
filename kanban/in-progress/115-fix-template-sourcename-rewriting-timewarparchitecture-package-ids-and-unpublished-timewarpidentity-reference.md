# Fix template sourceName rewriting TimeWarp.Architecture package ids and unpublished TimeWarp.Identity reference

## Description

Found 2026-07-22 during the orchestrator's independent review of 114-002 (first template
generate+build in weeks). `dotnet new timewarp-architecture -n SmokeDefault` produces an app
whose restore fails with 54 NU1101s, ALL pre-existing (not 114-002):

1. Template `sourceName` is `TimeWarp.Architecture`, so the engine rewrites the PACKAGE IDS
   `TimeWarp.Architecture.Analyzers` / `.Attributes` / `.Generators` in csproj/CPM into
   `SmokeDefault.Analyzers` etc. — nonexistent packages. Broken since the analyzer-packages
   dual-mode (092) landed those references. Fix: exempt the package-reference literals from
   sourceName substitution (template.json replacement exclusions, or restructure the ids so
   substitution can't touch them), and add a template-output restore/build smoke to CI so this
   class of break can't ship silently again (JT's test-templates.yml is prior art).
2. `TimeWarp.Identity` package referenced by generated output but not yet published (104
   program library). Decide: publish, or template consumes it via foundationPackages-style
   dual-mode/source inclusion until publish.

## Checklist

- [ ] Exempt TimeWarp.Architecture.* package ids from sourceName substitution; regenerate + restore green
- [ ] Resolve TimeWarp.Identity availability for generated apps (publish or dual-mode)
- [ ] Template smoke (generate + restore + build, both postgres states) wired into CI
- [ ] Both flag states build 0/0 from generated output

## Notes

Discovered via 114-002 review closing its 'template smoke not run' gap; smoke scripts/logs in session scratchpad. TWA0015/16 note: generated apps get grammar msbuild guard immediately, but analyzer diagnostics only after TimeWarp.Architecture.Analyzers republishes (pins lag published — expected).

### Implementation plan (2026-07-22, Phase 2)

**Status:** Ready to execute. No blocking open questions.

#### Locked decisions

1. Do **not** rename published `TimeWarp.Architecture.{Analyzers,Generators,Attributes}` PackageIds.
2. Survive `sourceName` by never writing the continuous substring `TimeWarp.Architecture` in
   package-id / platform-namespace **literals** that must stay platform-fixed; compose via MSBuild
   (`TimeWarp` + `.Architecture.*` property composition).
3. **Identity:** ship source into generated apps until first nuget.org publish — new
   `identityPackages` dual-mode, **default false**. Publish is ops follow-through, not a code gate.
4. **CI smoke** must generate with a name **≠** `TimeWarp.Architecture` (e.g. `SmokeDefault`) so
   rewrite bugs surface.
5. Smoke matrix: defaults + `--postgres false` → restore + build 0/0.

#### Part A — sourceName-safe package IDs

- Add `msbuild/timewarp-platform-packages.props` with composed IDs:
  `$(_TwPlatformVendor).Architecture.Analyzers` etc. + Attributes namespace property.
- Import from root `Directory.Build.props` and use in `Directory.Packages.props` + all
  PackageReference consumers (source/tests Directory.Build.props, web-server, api-server,
  web-spa, web-domain, web-contracts, api-contracts, timewarp-identity).
- Remove `global using TimeWarp.Architecture.Attributes;` from web/api contracts global-usings;
  add dual-mode MSBuild `<Using>` (package mode → platform namespace property; source mode →
  `$(RootNamespace).Attributes`).

#### Part B — TimeWarp.Identity dual-mode

- Add `UseIdentityPackages` detection (source present? false : true).
- Gate web-contracts / web-application / web-infrastructure Identity refs on
  `UseIdentityPackages` (not `UseFoundationPackages`).
- template.json: `identityPackages` default **false**; remove identity from
  `foundationPackages` exclude; exclude identity source only when `identityPackages=true`.
- Split slnx `#if` so identity nests under `!identityPackages`.

#### Part C — CI smoke

- New `dev template-smoke`: pack template → install → generate `SmokeDefault` (defaults) +
  `SmokeNoPostgres` (`--postgres false`) → restore + build -warnaserror; assert package IDs
  not rewritten.
- Wire `.github/workflows/template-smoke.yml` (PR + push, path-filtered) or parallel job.

#### Sequencing

1. A1–A3 + monorepo `dev build` 0/0  
2. B1–B3 + local generate restore  
3. Local full smoke both postgres states  
4. C1–C2 CI  
5. AGENTS.md dual-mode docs  
6. Ops: publish Identity later (not gate)

#### Out of scope

Rename product namespaces/sourceName; rename published PackageIds; full generated-app test suite;
Foundation pin bumps; Identity publish as only fix without dual-mode source path.

## Session

- Created: 2026-07-22 (from 114-002 template smoke gap)
- Orchestration: 2026-07-22 — Phase 1 in-progress; Phase 2 plan locked (above)
