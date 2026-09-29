# Update outdated NuGet packages (44 as of 2026-09-23)

## Description

`ganda nuget outdated` reports 44 outdated pins in `Directory.Packages.props`: 1 major, 14 minor,
29 patch. Bring them current in one pass, treating the one major as its own decision. Re-run
`ganda nuget outdated` at the start — the list below is the 2026-09-23 snapshot and will have
moved.

## Requirements

- **Patch (29):** take all. .NET 10.0.11 → 10.0.12 family (ASP.NET Core, EF Core, Extensions,
  System.Formats.Cbor, System.Security.Cryptography.Xml), `Microsoft.CodeAnalysis.NetAnalyzers`
  10.0.400 → 10.0.401, MessagePack 3.1.8 → 3.1.9, Scalar.AspNetCore 2.17.1 → 2.17.8,
  libphonenumber-csharp 9.0.37 → 9.0.39, TimeWarp.Terminal 1.0.1 → 1.0.2.
- **Minor (14):** take all, but read release notes for the two families that touch wire/runtime
  behavior and record anything relevant in Results: Grpc.* 2.83.0 → 2.84.0 (5 pins);
  OpenTelemetry.* 1.18.0 → 1.19.x (5 pins — check the exporter/hosting 1.19.1 vs instrumentation
  1.19.0 split is intentional upstream, not a partial publish); Microsoft.Extensions.Telemetry.Abstractions
  and Diagnostics.Testing 10.9.0 → 10.10.0; Testcontainers.PostgreSql 4.14.0 → 4.15.0 (re-check the
  SSH.NET transitive lift pin in CPM is still needed or can go); timewarp-simple-icons 16.27.1 → 16.32.0.
- **Major (1) — decision, not a bump:** `Microsoft.OpenApi` 2.12.2 → 3.10.2. Determine what pulls
  it in (Scalar / `Microsoft.AspNetCore.OpenApi`?), whether 3.x is compatible with the current
  ASP.NET Core 10 OpenAPI stack, and either take it with the breaking-change notes in Results or
  leave it pinned with a one-line reason and open a follow-up. Do not let a transitive conflict
  force a downgrade elsewhere.
- **Do NOT touch** the platform pins that equal the release `<Version>`
  (`TimeWarp.Foundation.*`, `TimeWarp.Modules`, `TimeWarp.Identity`, `TimeWarp.402`,
  `$(TwArchitecture*PackageId)`) — task 124 policy: those bump with the release, not here.
  Other first-party TimeWarp pins (Nuru, Amuru, State, Mediator, Jaribu, SourceGenerators,
  Terminal, Components, Multiavatar) follow the normal rule: never backward, check for package
  splits, migrate forward.
- Use `ganda nuget outdated --update` where it applies the pin edits cleanly; hand-edit only
  what it cannot. Keep CPM the single source — no `VersionOverride`.
- Gates: `dev build` 0/0 (analyzer bump ⇒ full rebuild; new NetAnalyzers rules may fire — fix,
  do not blanket-suppress), `dev test`, `dev template-smoke` (generated apps restore the same
  pins), `ganda repo audit`. If the EF Core patch changes the model snapshot, there must be no
  new migration required (`dev db-status` or the aspire migration resource) — record the check.
- Results: the final `ganda nuget outdated` output (should be 0 or only the deferred major),
  release-note findings for gRPC/OTel/OpenApi, and any analyzer rule fixes made.

## Checklist

- [x] Fresh `ganda nuget outdated` snapshot recorded
- [x] 29 patch pins updated
- [x] 14 minor pins updated; gRPC + OTel notes read; SSH.NET lift re-evaluated
- [x] Microsoft.OpenApi 3.x decided (taken with notes, or deferred with reason + follow-up)
- [x] Platform release pins untouched
- [x] `dev build` 0/0 · `dev test` · `dev template-smoke` · `ganda repo audit`
- [x] EF model snapshot unchanged / no pending migration

## Results

### Snapshot (2026-09-29)

The list had moved since 2026-09-23: **53 outdated** (2 major, 16 minor, 34 patch, 1 rc→stable).
New since the task was written: FluentUI Components `5.0.0-rc.5-26219.1 → 5.0.0` (GA),
FluentUI Icons `4.14.4 → 5.0.0` (major), TimeWarp.Amuru / Amuru.Tools `→ 1.1.1`,
TimeWarp.Mediator.* `14.0.0-beta.1 → beta.4`, TimeWarp.State / State.Plus `12.0.0-beta.3 → beta.5`;
several patch targets also moved further (MessagePack 3.1.10, Scalar 2.17.11, libphonenumber 9.0.40).

`ganda nuget outdated --update --force` applied all 53 pin edits; the only hand edits were
reverting `Microsoft.OpenApi` and comments. Platform release pins (`TimeWarp.Foundation.*`,
`TimeWarp.Modules`, `TimeWarp.Identity`, `TimeWarp.402`, `$(TwArchitecture*PackageId)`) are
untouched at `2.0.0-beta.20`. No `VersionOverride`. Also bumped the local `dotnet-ef` tool
10.0.10 → 10.0.12 to match the EF Core train.

Final `ganda nuget outdated`:

```
[67/114] Microsoft.OpenApi 2.12.2 -> 3.10.2
1 outdated package(s) (1 major)
```

### Decisions and release-note findings

- **Microsoft.OpenApi 3.x — deferred.** `Microsoft.AspNetCore.OpenApi` 10.0.12's nuspec still
  declares `Microsoft.OpenApi [2.12.0, 3.0.0)`; a 3.x pin is NU1608 (warning-as-error). Stays on
  2.12.2; CPM comment updated. Follow-up: **task 257** (published to to-do, gated on an
  AspNetCore.OpenApi that allows 3.x).
- **FluentUI Icons 5.0.0 / Components 5.0.0 GA — taken.** Clean build, web-spa bUnit/integration
  suites and template smoke green with no source changes.
- **gRPC 2.84.0** (5 pins): no API changes. Relevant fixes: grpc-web client no longer blocks on
  `SemaphoreSlim.Wait(0)` on single-threaded browser WASM (grpc-dotnet#2756); corrupted bytes after
  HTTP/2 GOAWAY fixed (#2766). No wire-format changes.
- **OpenTelemetry 1.19.x** (5 pins): exporter/hosting 1.19.1 vs instrumentation 1.19.0 is the
  intended upstream split — the core repo shipped a 1.19.1 patch (net8.0 wildcard source/meter
  `NotSupportedException`/OOM, #7788); contrib instrumentation's latest is 1.19.0 and floors core
  at `[1.19.0, 2.0.0)`. Behaviour notes from 1.19.0: OTLP exporter drops (rather than fails on)
  attributes that throw during serialization; dictionary-shaped attributes serialize as OTLP
  `kvlist`; exporter disables HttpClientFactory integration on browser WASM; inbound `tracestate`
  parsing fix; Schema URL added to internal `Resource`s.
- **SSH.NET lift — removed.** Testcontainers 4.15.0 depends on `SSH.NET 2026.0.0` directly, so the
  CPM `SSH.NET` pin and both direct references (`timewarp-testing`, `web-infrastructure-tests`) are
  gone; restore stays clean of NU1903.
- **TimeWarp.State 12.0.0-beta.5** (first-party, migrate forward):
  - removed its hand-copied `CamelCase` helper (timewarp-state#594) — the three `Hydrate`
    overrides (`CounterState`, `ApplicationState`, `WeatherForecastsState`) now use
    `JsonNamingPolicy.CamelCase.ConvertName`;
  - made `ThrowIfNotTestAssembly` case-insensitive (timewarp-state#607 fixed, #610) — the local
    `TestCaller` shim (`web-spa/features/base/test-caller.cs`) is deleted and the four debug
    seeders call State's `ThrowIfNotTestAssembly` again; Design regions reconciled.
- **NetAnalyzers 10.0.401:** no new diagnostics fired; no suppressions added.
- **MessagePack 3.1.10:** CPM comment updated; aspire-tests (StreamJsonRpc/DCP path) green.

### Gates

- `dev build --clean`: **0 warnings / 0 errors** (full rebuild for the analyzer bump).
- `dev test`: 22 projects, **1450 tests, 0 failed**, 1449 passed, 1 skipped (pre-existing skip in
  web-server-integration-tests).
- `dev template-smoke`: **SUCCEEDED** — SmokeDefault, SmokeNoPostgres, SmokeNoApi each 0/0 build,
  generated Jaribu aggregators and co-located runfiles pass.
- EF: `dotnet ef migrations has-pending-model-changes --context PostgresDbContext` →
  "No changes have been made to the model since the last migration." (no AppHost booted).
- `ganda repo audit`: passes all checks (after `--fix --checks bin-dev` built `bin/dev`).

### How to validate

- **Smoke:** `ganda nuget outdated && ./bin/dev build --clean && ./bin/dev test && ./bin/dev template-smoke`
- **Expect:** outdated lists only `Microsoft.OpenApi 2.12.2 -> 3.10.2`; build 0 warnings / 0 errors;
  every test project reports `failed: 0`; template smoke ends `Template smoke SUCCEEDED`;
  `grep -rn "TestCaller\|MemberNameToCamelCase\|SSH.NET" source tests Directory.Packages.props`
  returns nothing.

## Notes

- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
- Implemented: ganda task work implement oracle (2026-09-29) — pins, State beta.5 migration, gates, follow-up 257
