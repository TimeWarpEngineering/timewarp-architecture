# Bring all NuGet pins current: Mediator 15.0.0-beta.10, State 12.0.0-beta.19, Contracts 2.0.0-beta.34 and the rest of the outdated set

## Description

Since task 492 (2026-11-16) the upstream packages moved. Bring every pin in
`Directory.Packages.props` to the current version `ganda nuget outdated` reports, except the
EF Core family and the Npgsql provider, which task 494 moves in the same work-set before this
task runs. Never backward-pin.

### Pins to move (from `ganda nuget outdated`, 2026-11-22)

| Package | From | To |
|---|---|---|
| TimeWarp.Mediator, TimeWarp.Mediator.Generators | 15.0.0-beta.9 | 15.0.0-beta.10 |
| TimeWarp.State, .Blazor, .Plus, .Telemetry, .DevTools.Export, .DevTools.Hosting, .DevTools.Wasm | 12.0.0-beta.17 | 12.0.0-beta.19 |
| TimeWarp.Contracts | 2.0.0-beta.33 | 2.0.0-beta.34 |
| TimeWarp.SourceGenerators | 1.0.0-beta.16 | 1.0.0-beta.17 |
| TimeWarp.Jaribu, TimeWarp.Jaribu.TestingPlatform | 1.0.0-beta.16 | 1.0.0-beta.18 |
| TimeWarp.Nuru, TimeWarp.Nuru.DevCli | 4.0.0-beta.15 | 4.0.0-beta.16 |
| TimeWarp.Amuru, TimeWarp.Amuru.Tools | 3.0.0-beta.5 | 3.0.0-beta.10 |
| Microsoft.Extensions.* (Telemetry.Abstractions, ServiceDiscovery.Yarp, AI, AI.OpenAI, Compliance.Redaction, Http.Resilience, ServiceDiscovery, Telemetry, Diagnostics.Testing) | 11.0.0 | 11.0.1 |
| Microsoft.OpenApi | 3.11.0 | 3.11.1 (confirm it still satisfies Microsoft.AspNetCore.OpenApi's range; the pin comment says [3.11.0, 4.0.0)) |
| Scalar.AspNetCore | 2.21.0 | 3.1.1 (major: read the release notes, fix call sites) |
| Microsoft.FluentUI.AspNetCore.Components, .Icons | 5.0.1 | 5.1.0 |
| Microsoft.Playwright | 1.64.1 | 1.65.1 (keep the Playwright browser install in step) |
| Azure.Storage.Blobs | 12.30.1 | 12.30.2 |
| Testcontainers.PostgreSql | 4.16.0 | 4.17.0 |

If a later version is current when the task runs, use it. Lines reported as `$(AspireVersion)`
or `$(Tw…PackageId)` are property-driven and stay as they are unless `aspire update` says the
Aspire line moved; the Aspire CLI must then match `<AspireVersion>`.

### What the TimeWarp bumps bring

- Mediator beta.10: `GeneratedRequestPatternAttribute` and TWM013 (additive). No call-site change expected.
- State beta.18/19: declared actions (TWS0015, manifest origin/handler/response fields, dispatch
  for declared actions), Roslyn 5.12 floor, Mediator beta.10 pin. Read
  `documentation/release-notes/release12.0.0-beta.18.md` and `-beta.19.md` in timewarp-state;
  fix any new diagnostic at source, not by suppression. Do not adopt `[ContractState]` or
  declared actions here; that is task 483.
- Contracts beta.34: Mediator beta.10 pin only.
- SourceGenerators beta.17, Jaribu beta.18, Nuru/DevCli beta.16, Amuru beta.10: read release
  notes for any analyzer or API change and fix at source.

## Checklist

- [ ] All pins above moved; `ganda nuget outdated` reports 0 (after 494 has landed first)
- [ ] Scalar 3.x and any other major: release notes read, call sites fixed, note in the pin comment
- [ ] `dev build` 0 warnings / 0 errors; new diagnostics fixed at source
- [ ] `dev test`
- [ ] `dev template-smoke --matrix`
- [ ] Playwright suite passes with the new Playwright pin
- [ ] `dev check-version`: bump `<Version>` to 2.0.0-beta.33 and the platform package pins in the same commit (library pins change, so a release follows; 2.0.0-beta.32 was never cut, so the bump may be to beta.32 if `check-version` accepts it; follow its output)
- [ ] `ganda repo audit` clean
- [ ] No cross-repo work; stop and report if an upstream package needs a fix

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-22)

## Notes

- Filed per Steve 2026-11-22 ("file the task and lets do the updates"). Runs after 494 in one
  work-set so both edit `Directory.Packages.props` serially.
- Unblocks task 483 (needs Mediator beta.10 and State beta.19 pinned).
