# Upgrade timewarp-architecture to .NET 11

## Description

Bump **timewarp-architecture** from **.NET 10 / net10.0** to **.NET 11 / net11.0** (SDK 11.0.100-rc.1 go-live as of 2026-10-02; GA expected ~2026-11-10).

**This task is analysis + ordered upgrade planning only until picked up for implementation.** Do **not** implement product code, open a product PR, or start the TFM bump until an agent claims implementation work (prefer GA; RC1 is go-live if Steve authorizes early).

Related existing tasks (do not duplicate blindly):
- **267** — merged into this task 2026-10-04 (cockpit, per Steve). This task is the single .NET 11 owner, including Aspire `AddDotnetProject()` (step 13).
- **257** — Microsoft.OpenApi 3.x (gated on AspNetCore.OpenApi .NET 11 train).
- **271** — net11 agentic UI over TimeWarp.State action catalog (depends on net11 landing).

## Requirements

1. Follow the **ordered dependency list** in Notes (first → later). Do not bump TFMs before SDK / CI images are ready.
2. Mirror root `global.json` SDK pin into **all 21** project-local `tests/**/global.json` files (Jaribu MTP / timewarp-jaribu#20).
3. Central TFM lives in root `Directory.Build.props` (`net10.0` today). Also fix outliers: template pack csproj still `net9.0`; evals fixture `net10.0`.
4. Move Microsoft.* / ASP.NET / EF / Extensions / NetAnalyzers CPM pins from the **10.0.x** train to **11.x**; bump `dotnet-ef` local tool; keep TimeWarp first-party pins forward-only (fix upstream if needed).
5. Update CI `actions/setup-dotnet` (`10.0.x` → `11.0.x`), grpc Dockerfile `ARG dotnet_version=10.0`, Aspire skill docs that hardcode .NET 10.
6. Revisit in-repo .NET 11 trackers: Microsoft.OpenApi 3.x (task 257), web-spa TypeScript/StaticWebAssets workaround (tracked for .NET 11), Aspire `AddDotnetProject()` (step 13, from 267).
7. Gates before done (implementation phase only): `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`, `dev check-version` (version + pins same commit, policy 124).

## Checklist

### Preflight (before any code)
- [ ] Confirm SDK available on TWE-001 / CI (prefer GA 11.0.x; else RC1 go-live with Steve OK)
- [x] Ownership vs task **267** decided: 267 merged into 272 (2026-10-04); 272 owns AddDotnetProject

### Ordered upgrade steps (implement in this order)
- [ ] 1. Root `global.json` SDK pin (`10.0.400` → `11.0.100-rc.1` or GA) + `rollForward`
- [ ] 2. Mirror SDK pin in all **21** `tests/**/global.json` (keep `test.runner: Microsoft.Testing.Platform`)
- [ ] 3. CI: `.github/workflows/workflow.yml` `dotnet-version: '10.0.x'` → `11.0.x` (both `ci` and `template-smoke` jobs)
- [ ] 4. Docker: `source/container-apps/grpc/.../Dockerfile` `ARG dotnet_version=10.0` → `11.0` (mcr sdk/aspnet jammy tags)
- [ ] 5. TFM: root `Directory.Build.props` `net10.0` → `net11.0`; `evals/contracts/fixtures/web-contracts.csproj`; template pack `timewarp-architecture-template.csproj` (`net9.0` → `net11.0`)
- [ ] 6. Local tools: `.config/dotnet-tools.json` `dotnet-ef` `10.0.12` → 11.x; consider `microsoft.dotnet-httprepl` (still 8.0.0)
- [ ] 7. CPM `Directory.Packages.props`: all `Microsoft.AspNetCore.*` / `Microsoft.EntityFrameworkCore*` / `Microsoft.Extensions.*` / `System.*` on 10.0.12 → 11.x; `Microsoft.CodeAnalysis.NetAnalyzers` 10.0.401 → 11-aligned; Aspire.Hosting.* / AppHost Sdk 13.6.0 as needed for net11
- [ ] 8. Sibling TimeWarp packages (Nuru, Amuru, State, Mediator, Jaribu, SourceGenerators, Components, …): confirm net11 support; bump pins forward only — never pin backward
- [ ] 9. Analyzers / Roslynator / BannedApi / CodeStyle — verify RS1041 and analyzer TFM comments (convention-analyzers intentionally target TFM for CLI)
- [ ] 10. Docs/skills/samples: AGENTS.md ".NET 10", Aspire skill SDK tables, web-spa `index.md`, safety-guardrail `bin/Debug/net10.0` examples
- [ ] 11. Unblock / coordinate **257** (OpenApi 3.x) once AspNetCore.OpenApi 11 declares OpenApi >=3
- [ ] 12. Revisit web-spa TypeScript early-compile workaround vs `Microsoft.NET.Sdk.StaticWebAssets.TypeScript.targets` (.NET 11 tracker in web-spa.csproj)
- [ ] 13. Aspire `AddDotnetProject()` (`Aspire.Hosting.Dotnet`), folded in from 267. Adopt it once it is
      out of prerelease; if it is still prerelease, record that and ask Steve. Rewrite the
      `AddProject<Projects.*>` calls, adjust the `Projects.*` typed `ProjectReference` wiring, and
      reconcile TWA0007 (resource names = `ServiceNames`) with the new API. The CLI skill
      `aspire-project-v2-migration` may help, but review its output. The point is coordinated
      restore and multi-threaded MSBuild on the .NET 11 SDK.
- [ ] 14. Fix all new net11 warnings-as-errors; no blanket suppressions
- [ ] 15. Version bump + CPM first-party pins same commit; `dev template-smoke` + publish path

### Start condition
- Prefer .NET 11 **GA** (~2026-11-10). Start on RC1 only if Steve authorizes it, and record it as
  prerelease.
- Land task **070-002** (Dockerfile removal) first if it is still open. Step 4 then has nothing
  to bump.
- Do **not** start an AppHost. A full-solution TFM bump is a heavy build: run serially and call
  `dotnet build-server shutdown` before finishing.

## Session

- Created: 501620 (2026-10-02)
- Analysis inventory: Architecture / Grok executor on TWE-001 master worktree
- 2026-10-04: task 267 merged in (AddDotnetProject → step 13); 271 now depends on 272 (cockpit, per Steve)

## Notes

### Current state (inventory 2026-10-02)

| Area | Current | Target |
|------|---------|--------|
| Root SDK (`global.json`) | 10.0.400, rollForward latestFeature | 11.0.x (RC1 or GA) |
| Project-local `global.json` | 21 files under `tests/**`, all 10.0.400 + MTP runner | Mirror root 11.0.x |
| Default TFM (`Directory.Build.props`) | net10.0 | net11.0 |
| Template pack csproj | **net9.0** (stale outlier) | net11.0 |
| Evals fixture | net10.0 | net11.0 |
| CI setup-dotnet | 10.0.x (ci + template-smoke) | 11.0.x |
| grpc Dockerfile | ARG 10.0 → mcr sdk/aspnet jammy | 11.0 |
| `dotnet-ef` tool | 10.0.12 | 11.x |
| httprepl tool | 8.0.0 | review |
| Microsoft.* CPM | mostly 10.0.12 / 10.10.0 | 11.x |
| Aspire.AppHost.Sdk | 13.6.0 | confirm net11-compatible train |
| NetAnalyzers | 10.0.401 | 11-aligned |
| No nuget.config at repo root | (none found) | n/a |

### Dependency order — update FIRST → later

1. **global.json / SDK roll-forward** (root) — nothing builds without the SDK
2. **Project-local global.json mirrors** (21 under tests/) — Jaribu aggregators break if mismatched
3. **CI setup-dotnet + runner** (`.github/workflows/workflow.yml` 10.0.x → 11.0.x) — gates must restore SDK before merge
4. **Docker base images** (grpc Dockerfile `dotnet_version`) — container builds lag TFM otherwise
5. **Directory.Build.props TFM** (+ template pack net9.0 outlier + evals fixture)
6. **Microsoft.* / ASP.NET / EF / Extensions / System.* CPM** aligned to 11
7. **dotnet-tools.json** (dotnet-ef; httprepl)
8. **Analyzers / NetAnalyzers / Roslynator / CodeAnalysis.***
9. **Aspire SDK + Hosting packages** (and AddDotnetProject when ready — step 13, from 267)
10. **TimeWarp sibling package pins** (forward-only; upstream TFM support first)
11. **OpenApi 3.x** (task 257 — blocked until AspNetCore.OpenApi 11 allows it)
12. **web-spa TypeScript/StaticWebAssets** cleanup if SDK ships the tracked targets
13. **Docs / skills / AGENTS.md / samples** mentioning .NET 10 / net10.0
14. **Release version + template-smoke**

### Known RC / in-repo caveats

- `Directory.Packages.props` explicitly defers **Microsoft.OpenApi 3.x** to the .NET 11 train (AspNetCore.OpenApi 10.0.12 still `[2.12.0, 3.0.0)`).
- web-spa.csproj documents a TypeScript compile-before-StaticWebAssets workaround; comment says tracked for .NET 11 SDK StaticWebAssets.TypeScript.targets.
- Analyzer projects suppress RS1041 and intentionally inherit TFM for CLI usage — re-validate on net11.0.
- tests/Directory.Build.props documents NU1608 transitive ceilings (Oakton / AutoFixture.AutoFakeItEasy) that lag the framework train — re-check on 11.
- Interceptors property already uses `InterceptorsNamespaces` (.NET 10 rename) — unlikely to regress.
- Aspire skills under `.agents/skills` and `.claude/skills` hardcode ".NET 10.0 SDK" / `dotnet-version: 10.0.x` in examples.
- No root `nuget.config` / `Packages.props` (only `Directory.Packages.props`).
- RC1 is go-live; prefer waiting for GA (~Nov 10, 2026) unless Steve authorizes RC.

### Blockers

- Machine/CI must have SDK 11 installed or setup-dotnet must resolve `11.0.x` (incl. preview quality if RC).
- Task **267** merged in (2026-10-04); no overlap remains.
- First-party TimeWarp packages without net11 TFM support must be fixed upstream first.

## Results

*(fill when implementation is done)*

### How to validate

*(required before done — implementation phase)*

**Smoke:**

```bash
dotnet --version   # expect 11.0.x
./bin/dev build
./bin/dev test
./bin/dev template-smoke
```

**Expect:** 0/0 build warnings/errors on net11.0; Jaribu suites green; template pack installs and builds generated app on net11.0.
