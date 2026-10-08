# Upgrade timewarp-architecture to .NET 11

## Description

Bump **timewarp-architecture** from **.NET 10 / net10.0** to **.NET 11 / net11.0** (SDK 11.0.100-rc.1 go-live as of 2026-10-02; GA expected ~2026-11-10).

**This task is analysis + ordered upgrade planning only until picked up for implementation.** Do **not** implement product code, open a product PR, or start the TFM bump until an agent claims implementation work (prefer GA; RC1 is go-live if Steve authorizes early).

Related existing tasks (do not duplicate blindly):
- **267** — merged into this task 2026-10-04 (cockpit, per Steve). This task is the single .NET 11 owner, including Aspire `AddDotnetProject()` (step 13).
- **257** — Microsoft.OpenApi 3.x (gated on AspNetCore.OpenApi .NET 11 train).
- **271** — net11 agentic UI over TimeWarp.State action catalog (depends on net11 landing).

## Authorization (Steve, 2026-10-09 ~02:00 ICT)

Steve asked for this task to be **implemented and merged tonight** (overnight run, serial 272 → 271 → 273). That is the authorization the Start condition asks for:

- **Implement now on .NET 11 RC1** (`11.0.100-rc.1.26425.128`, installed on TWE-001). Record it as prerelease in Results. Do not wait for GA.
- The "analysis + planning only" line above no longer applies: do the ordered upgrade steps, open the PR.
- **Step 13 (`AddDotnetProject`)**: if `Aspire.Hosting.Dotnet` is still prerelease or does not work on 13.6.x, record that in Results and leave step 13 as a follow-up note for Steve. Do **not** block the walk on it.
- **Step 11 (task 257)**: coordinate only; take OpenApi 3.x only if AspNetCore.OpenApi 11 RC allows it cleanly, else leave 257 open.
- TimeWarp sibling packages without a `net11.0` asset are not a blocker (net11.0 consumes their net10.0 assets). Keep pins forward-only.
- CI must go green on `11.0.x` (setup-dotnet may need `dotnet-quality: preview` for RC).
- PR body must carry proof: `dotnet --version`, `dev build` 0/0 summary, `dev test` summary, `dev template-smoke` summary, `ganda repo audit` result.

## Requirements

1. Follow the **ordered dependency list** in Notes (first → later). Do not bump TFMs before SDK / CI images are ready.
2. Mirror root `global.json` SDK pin into **all 21** project-local `tests/**/global.json` files (Jaribu MTP / timewarp-jaribu#20).
3. Central TFM lives in root `Directory.Build.props` (`net10.0` today). Also fix outliers: template pack csproj still `net9.0`; evals fixture `net10.0`.
4. Move Microsoft.* / ASP.NET / EF / Extensions / NetAnalyzers CPM pins from the **10.0.x** train to **11.x**; bump `dotnet-ef` local tool; keep TimeWarp first-party pins forward-only (fix upstream if needed).
5. Update CI `actions/setup-dotnet` (`10.0.x` → `11.0.x`) and Aspire skill docs that hardcode .NET 10. (No Dockerfiles remain — task 070-002; SDK container build follows the TFM.)
6. Revisit in-repo .NET 11 trackers: Microsoft.OpenApi 3.x (task 257), web-spa TypeScript/StaticWebAssets workaround (tracked for .NET 11), Aspire `AddDotnetProject()` (step 13, from 267).
7. Gates before done (implementation phase only): `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`, `dev check-version` (version + pins same commit, policy 124).

## Checklist

### Preflight (before any code)
- [x] Confirm SDK available on TWE-001 / CI (prefer GA 11.0.x; else RC1 go-live with Steve OK)
- [x] Ownership vs task **267** decided: 267 merged into 272 (2026-10-04); 272 owns AddDotnetProject

### Ordered upgrade steps (implement in this order)
- [x] 1. Root `global.json` SDK pin (`10.0.400` → `11.0.100-rc.1` or GA) + `rollForward`
- [x] 2. Mirror SDK pin in all **21** `tests/**/global.json` (keep `test.runner: Microsoft.Testing.Platform`)
- [x] 3. CI: `.github/workflows/workflow.yml` `dotnet-version: '10.0.x'` → `11.0.x` (both `ci` and `template-smoke` jobs)
- [x] 4. Container images: no Dockerfiles remain (deleted by task 070-002) — `aspire publish` uses the .NET SDK container build, whose aspnet base image follows the TFM. Only confirm no `ContainerBaseImage`/`ContainerFamily` pin was added to a server csproj since; if one was, bump it to the 11.0 tag
- [x] 5. TFM: root `Directory.Build.props` `net10.0` → `net11.0`; `evals/contracts/fixtures/web-contracts.csproj`; template pack `timewarp-architecture-template.csproj` (`net9.0` → `net11.0`)
- [x] 6. Local tools: `.config/dotnet-tools.json` `dotnet-ef` `10.0.12` → 11.x; consider `microsoft.dotnet-httprepl` (still 8.0.0)
- [x] 7. CPM `Directory.Packages.props`: all `Microsoft.AspNetCore.*` / `Microsoft.EntityFrameworkCore*` / `Microsoft.Extensions.*` / `System.*` on 10.0.12 → 11.x; `Microsoft.CodeAnalysis.NetAnalyzers` 10.0.401 → 11-aligned; Aspire.Hosting.* / AppHost Sdk 13.6.0 as needed for net11
- [x] 8. Sibling TimeWarp packages (Nuru, Amuru, State, Mediator, Jaribu, SourceGenerators, Components, …): confirm net11 support; bump pins forward only — never pin backward
- [x] 9. Analyzers / Roslynator / BannedApi / CodeStyle — verify RS1041 and analyzer TFM comments (convention-analyzers intentionally target TFM for CLI)
- [x] 10. Docs/skills/samples: AGENTS.md ".NET 10", Aspire skill SDK tables, web-spa `index.md`, safety-guardrail `bin/Debug/net10.0` examples
- [x] 11. Unblock / coordinate **257** (OpenApi 3.x) once AspNetCore.OpenApi 11 declares OpenApi >=3
- [x] 12. Revisit web-spa TypeScript early-compile workaround vs `Microsoft.NET.Sdk.StaticWebAssets.TypeScript.targets` (.NET 11 tracker in web-spa.csproj)
- [x] 13. Aspire `AddDotnetProject()` (`Aspire.Hosting.Dotnet`), folded in from 267. Adopt it once it is
      out of prerelease; if it is still prerelease, record that and ask Steve. Rewrite the
      `AddProject<Projects.*>` calls, adjust the `Projects.*` typed `ProjectReference` wiring, and
      reconcile TWA0007 (resource names = `ServiceNames`) with the new API. The CLI skill
      `aspire-project-v2-migration` may help, but review its output. The point is coordinated
      restore and multi-threaded MSBuild on the .NET 11 SDK.
- [x] 14. Fix all new net11 warnings-as-errors; no blanket suppressions
- [x] 15. Version bump + CPM first-party pins same commit; `dev template-smoke` + publish path

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
- 2026-10-09: implemented on RC1 `11.0.100-rc.1.26425.128` (Steve authorized). AddDotnetProject not adopted (still preview). Task 257 left open.

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
| Container base image | SDK container build (no Dockerfiles, task 070-002) — follows TFM | no edit unless a `ContainerBaseImage` pin exists |
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
4. **Container base images** — none pinned (SDK container build follows the TFM, task 070-002); check only for a later `ContainerBaseImage` pin
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

Implemented on **.NET 11 RC1** (prerelease). Steve authorized the RC1 go-live on 2026-10-09; GA (~2026-11-10) was not waited for. SDK pin is `11.0.100-rc.1.26425.128` with `rollForward: latestFeature`.

- Root `global.json` and all 21 `tests/**/global.json` files pin that SDK. Test copies keep `test.runner: Microsoft.Testing.Platform`.
- TFM is `net11.0` in root `Directory.Build.props`, `evals/contracts/fixtures/web-contracts.csproj`, and the template pack (`timewarp-architecture-template.csproj` was `net9.0`).
- CI `actions/setup-dotnet@v4` on both the `ci` and `template-smoke` jobs uses `dotnet-version: 11.0.x` and `dotnet-quality: preview` so the RC resolves.
- No Dockerfiles and no `ContainerBaseImage` / `ContainerFamily` pins. The SDK container build follows the TFM.
- Framework CPM (`Microsoft.AspNetCore.*`, EF Core, Extensions, `System.*`) is `11.0.0-rc.1.26425.128`. `Microsoft.CodeAnalysis.NetAnalyzers` is `11.0.100-rc.1.26425.128`. Monthly Extensions with no 11.x (`Http.Resilience`, `ServiceDiscovery`, `ServiceDiscovery.Yarp`, `Telemetry.Abstractions`, `Diagnostics.Testing`) stay `10.10.0`. `System.ServiceModel.Primitives` stays `10.0.652802`.
- `dotnet-ef` is `11.0.0-rc.1.26425.128`. `microsoft.dotnet-httprepl` stays `8.0.0` (no 11.x package).
- `Aspire.AppHost.Sdk` is `13.6.1`. `Aspire.Hosting.EntityFrameworkCore` and `Aspire.Hosting.Kubernetes` are `13.6.1-preview.1.26506.6` (the net11-capable train).
- TimeWarp sibling pins were not moved backward. Product version stays `2.0.0-beta.20` (one prerelease ahead of NuGet `2.0.0-beta.19`). Policy 124 holds: pins equal Version, and Version is ahead of the last release.
- NU1510: dropped direct PackageReferences to shared-framework assemblies. CPM pins for `Microsoft.Extensions.Logging.Abstractions` and `Microsoft.Extensions.Options` stay, because co-located runfiles take them with a versionless `#:package` directive. A csproj must not PackageReference them.
- IDE0211 ("convert to Program.Main") is suppressed on co-located Jaribu runfiles and on `tools/dev-cli` / `tools/agent-identity-cli`. Those programs are top-level so `dotnet run file.cs` keeps working (CI template-smoke and workflow use that path). `tools/dev-cli` also suppresses IDE0005, IDE0022, IDE0046, IDE0055, IDE0058, IDE0065, IDE0066, IDE0078, IDE0160, and IDE0290: they fire only in TimeWarp.Nuru.DevCli content files compiled into the runfile. Those sources live in the NuGet package, outside this repo.
- EF 11 verbose `dotnet ef` prints a full MSBuild item graph (~90s) before the database accepts queries. The site-settings seed budget is 180 attempts × 1s, quiet for the first 120. Aspire ingress health waits and the SPA integration host wait are 4 minutes; the first-run "Application started" watch is 3 minutes. No WaitFor / WaitForCompletion edge was added (task 270: that edge makes DCP fail `ASPNETCORE_URLS` substitution).
- web-spa TypeScript workaround stays. The SDK ships `Microsoft.NET.Sdk.StaticWebAssets.TypeScript.targets` only when `EnableTypeScriptNuGetTarget` is true, and `Microsoft.TypeScript.MSBuild` 7.0.1 does not set that property.
- **Step 13 follow-up for Steve:** `Aspire.Hosting.Dotnet` latest on NuGet is `13.6.1-preview.1.26506.6` (preview only, back through the 13.5 previews; no stable). `AddDotnetProject()` was not adopted. `AddProject<Projects.*>` stays.
- **Task 257 stays open.** `Microsoft.AspNetCore.OpenApi` 11 declares `Microsoft.OpenApi` `[3.10.0, 4.0.0)`. FastEndpoints.OpenApi 8.3.0 (latest stable) and 8.4.0-beta.22 still depend on AspNetCore.OpenApi 10.0.11, whose nuspec keeps OpenApi `[2.7.5, 3.0.0)`. The pin stays `2.12.2`. Taking 3.x is not a clean restore.

### How to validate

**Smoke:**

```bash
dotnet --version   # 11.0.100-rc.1.26425.128
./bin/dev build
./bin/dev test
dotnet run tools/dev-cli/dev.cs -- template-smoke
ganda repo audit
./bin/dev check-version
```

**Expect:**

- `dotnet --version` prints `11.0.100-rc.1.26425.128`.
- `./bin/dev build`: Build succeeded, 0 Warning(s), 0 Error(s), Time Elapsed 00:00:25.87, on `net11.0`.
- `./bin/dev test`: Tests completed successfully (exit 0). 21 projects, 1780 total, 0 failed, 1779 succeeded, 1 skipped (`web-server-integration-tests`). `aspire-tests` 38/38 in 2m 24s. `web-spa-integration-tests` 137/137 in 1m 40s.
- `dotnet run tools/dev-cli/dev.cs -- template-smoke` (the CI command): `Template smoke SUCCEEDED`, exit 0, about 228s. SmokeDefault, SmokeNoPostgres, and SmokeNoApi each built 0/0 on `net11.0`. Standalone co-located runfiles passed: create-role 5/5, hello 2/2, and get-weather-forecasts 5/5 on the api-on cells.
- `ganda repo audit`: Passed 31, Failed 0.
- `./bin/dev check-version`: source `2.0.0-beta.20` is 1 prerelease increment ahead of NuGet `2.0.0-beta.19`. "Version in source is new — safe to release." No version bump.
