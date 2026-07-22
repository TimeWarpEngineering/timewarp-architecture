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

- [x] Exempt TimeWarp.Architecture.* package ids from sourceName substitution; regenerate + restore green
- [x] Resolve TimeWarp.Identity availability for generated apps (publish or dual-mode)
- [x] Template smoke (generate + restore + build, both postgres states) wired into CI
- [x] Both flag states build 0/0 from generated output


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
- Implementer (2026-07-22): Parts A–C complete.
  - A: `msbuild/timewarp-platform-packages.props` composes package IDs/namespaces; all PackageReference/
    PackageVersion/PackageId consumers use properties; dual-mode MSBuild `<Using>` for Attributes +
    TypedId root namespace (file-level `using TimeWarp.Architecture` removed from id files).
  - B: `UseIdentityPackages` dual-mode (default source); template symbol `identityPackages` default
    false; slnx split (single libraries folder, independent identity gate).
  - C: `dev template-smoke` packs monorepo platform packages @ `2.0.0-smoke` + template, installs,
    generates SmokeDefault + SmokeNoPostgres, rewrites CPM pins, local NuGet.config with
    packageSourceMapping, restore+build 0/0. Workflow: `.github/workflows/template-smoke.yml`.
  - Bonus fix: feature-membership.targets Error condition was stripped by template engine (`&gt;` /
    empty-string item compares) → always-fail Error in generated apps; staged via property with raw `>`.
  - Verification: monorepo `dev build` 0/0; `dev template-smoke` SUCCEEDED both matrix cells.
  - Gaps (ops, not code gate): nuget.org foundation pins lag monorepo `Entity<TId>`; Attributes
    beta.5 on nuget lacks EndpointAllowAnonymous — smoke packs local monorepo content so CI is green;
    real nuget.org consumers need republish. Identity package still unpublished (source path default).
- Phase 4b review: 2026-07-22 — effort 1 general; 0 findings; disposition clean.
  Artifacts: `review/review-framework.md`, `review/round-1/`, `review/disposition.md`.

## Results

### What was implemented

1. **sourceName-safe platform package IDs** — `msbuild/timewarp-platform-packages.props` composes
   `TimeWarp` + `.Architecture.*` so package IDs are not rewritten when generating with a non-default
   app name. All PackageReference/PackageVersion/PackageId consumers use the properties.
2. **Dual-mode MSBuild `<Using>`** for Attributes (contracts) and TypedId root namespace (identity /
   web-domain); continuous `using TimeWarp.Architecture…` removed from product files that ship.
3. **`identityPackages` dual-mode** (default **false** = ship identity source until nuget.org
   publish). `UseIdentityPackages` auto-detect; independent of foundationPackages.
4. **`dev template-smoke` + CI** — packs monorepo platform packages @ `2.0.0-smoke`, generates
   `SmokeDefault` + `SmokeNoPostgres`, asserts package IDs intact, restore+build 0/0.
   Workflow: `.github/workflows/template-smoke.yml`.
5. **Bonus:** `feature-membership.targets` Error condition made template-engine-safe (raw `>` via
   property; empty-item compares stripped previously).

### Files changed (high level)

- `msbuild/timewarp-platform-packages.props` (new)
- `Directory.Build.props`, `Directory.Packages.props`, source/tests Directory.Build.props
- web/api/identity csprojs; `.template.config/template.json`; `timewarp-architecture.slnx`
- `tools/dev-cli/endpoints/template-smoke-command.cs`; `.github/workflows/template-smoke.yml`
- `feature-membership.targets`; `AGENTS.md`

### Key decisions / deviations

- Smoke packs **local monorepo packages** at unique version so nuget.org cache cannot shadow
  incomplete published surface — required for CI green without waiting on republish.
- Identity publish deferred (ops); source inclusion is the greenfield path.

### Test outcomes

| Check | Result |
|--------|--------|
| Monorepo `dev build` | 0/0 |
| `dev template-smoke` SmokeDefault | OK |
| `dev template-smoke` SmokeNoPostgres | OK |

### Phase 4b review

- **Effort:** 1 (general)
- **Rounds:** 1
- **Final counts:** 0 open, 0 fixed, 0 wontfix
- **Disposition:** clean
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`

### Remaining (ops, not blockers)

- Republish Foundation/Attributes so pure nuget.org consumers match monorepo surface
- First publish of `TimeWarp.Identity`; later flip `identityPackages` default true
