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

- [ ] Fresh `ganda nuget outdated` snapshot recorded
- [ ] 29 patch pins updated
- [ ] 14 minor pins updated; gRPC + OTel notes read; SSH.NET lift re-evaluated
- [ ] Microsoft.OpenApi 3.x decided (taken with notes, or deferred with reason + follow-up)
- [ ] Platform release pins untouched
- [ ] `dev build` 0/0 · `dev test` · `dev template-smoke` · `ganda repo audit`
- [ ] EF model snapshot unchanged / no pending migration

## Notes

- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
