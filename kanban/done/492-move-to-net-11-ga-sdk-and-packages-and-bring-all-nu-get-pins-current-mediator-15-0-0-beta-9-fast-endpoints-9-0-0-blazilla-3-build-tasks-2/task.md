# Move to .NET 11 GA and bring all NuGet pins current

## Description

Architecture is the last TimeWarp repo on the .NET 11 RC3 toolchain. `global.json` pins
`11.0.100-rc.3.26522.148`; every Microsoft.* pin is `11.0.0-rc.3.26522.148` while 11.0.0 GA is
out. The libraries we depend on have moved: TimeWarp.Mediator 15.0.0-beta.9 and the coming
TimeWarp.State 12.0.0-beta.18 compile their generators and analyzers against Roslyn 5.12.0 and
load only in the .NET 11 GA SDK compiler. Mediator already did this swap (timewarp-mediator task
023-001, commit b8432b5) and State did too (timewarp-state tasks 156 and 157, commits b4a19510 and
e4c304a8); read those diffs for the shape.

`ganda nuget outdated` on 2026-11-16 lists 62 outdated pins. Groups:

| Group | From | To |
|-------|------|----|
| SDK (`global.json`) | 11.0.100-rc.3.26522.148 | 11.0.100, `rollForward: latestFeature` |
| Microsoft.AspNetCore.*, Microsoft.EntityFrameworkCore.*, Microsoft.Extensions.* (DI, Http, Caching, Options, Logging), System.* | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.Extensions.{AI, AI.OpenAI, Telemetry, Telemetry.Abstractions, Compliance.Redaction, Http.Resilience, ServiceDiscovery, ServiceDiscovery.Yarp, Diagnostics.Testing} | 10.11.0 | 11.0.0 |
| Microsoft.CodeAnalysis.* (CSharp, Workspaces, CodeStyle, Analyzers) | 5.9.0 | 5.12.0 |
| Microsoft.CodeAnalysis.NetAnalyzers | 11.0.100-rc.3 | 11.0.100 |
| TimeWarp.Mediator, TimeWarp.Mediator.Generators | 15.0.0-beta.5 | 15.0.0-beta.9 (net11.0 only; dispatcher unchanged, no API change) |
| TimeWarp.Contracts | 2.0.0-beta.31 | 2.0.0-beta.33 (own version line; read its release notes) |
| TimeWarp.SourceGenerators | 1.0.0-beta.13 | 1.0.0-beta.16 |
| TimeWarp.Jaribu, TimeWarp.Jaribu.TestingPlatform | 1.0.0-beta.15 | 1.0.0-beta.16 |
| TimeWarp.Nuru, TimeWarp.Nuru.DevCli | 4.0.0-beta.12 | 4.0.0-beta.14 |
| TimeWarp.Amuru, TimeWarp.Amuru.Tools | 3.0.0-beta.2 | 3.0.0-beta.5 |
| TimeWarp.Terminal | 2.0.0-beta.1 | 2.0.0-beta.2 |
| TimeWarp.Build.Tasks | 1.0.0 | 2.0.0 (major; net11.0 only; `TWBT0001` on .NET Framework MSBuild, see Mediator beta.8 notes) |
| TimeWarp.Multiavatar | 1.0.0-beta.13 | 1.0.0-beta.15 |
| FastEndpoints, FastEndpoints.OpenApi | 9.0.0-beta.7 | 9.0.0 (GA; rewrite the pin comment that explains the beta pin) |
| Microsoft.AspNetCore.OpenApi | 11.0.0-rc.3 | 11.0.0 |
| Scalar.AspNetCore | 2.19.0 | 2.21.0 |
| Blazilla | 2.4.3 | 3.0.1 (major; read its release notes, fix every EditForm validator site: RoleForm, TodoItemForm, ProfilePage, AuthenticationPage) |
| OpenTelemetry.* (5 packages) | 1.20.x | 1.21.0 |
| timewarp-simple-icons | 16.36.0 | 16.37.0 |
| System.Security.Cryptography.Pkcs | 10.0.14 | 11.0.0 |

TimeWarp.State stays at 12.0.0-beta.17; it is the latest on NuGet. When beta.18 ships it is a
follow-up pin bump, not this task.

## Requirements

- Every row above at the listed version; `ganda nuget outdated` reports 0 outdated afterwards.
  Anything new it reports gets bumped too. Never backward-pin; if a bump breaks, fix forward or
  stop and report in `## Notes`.
- `global.json` on 11.0.100 GA. CI runs on the SDK `global.json` names; confirm `workflow.yml`
  and `dev workflow` need no change, or change them with the pin.
- Aspire: `<AspireVersion>` and the Aspire CLI must agree (`tw-deploy`); bump only if outdated
  lists it.
- Majors: Blazilla 3.0.1 and TimeWarp.Build.Tasks 2.0.0. Read their release notes before bumping
  and list every code change they forced in Results. Audit `--fix` output is policy and rides
  along.
- `dev build` 0 warnings 0 errors. New analyzer diagnostics from the newer Roslyn, NetAnalyzers
  11.0.100, SourceGenerators beta.16 or Mediator beta.9 are fixed at the source, never suppressed;
  list each rule id and fix in Results.
- `dev test`, `dev template-smoke --matrix` (SDK and package changes alter every template
  variant), Playwright, and `dev check-version` pass. `dev check-version`: 2.0.0-beta.32 is
  unreleased; bump `<Version>` and the platform pins that equal it only if it says so.
- `ganda repo audit` clean.
- Do not open tasks or PRs in other repos. If a TimeWarp package needs a fix upstream, stop and
  report.

## Goal (implementer restatement)

Move the repo off the .NET 11 RC3 toolchain to .NET 11 GA (SDK 11.0.100) and bring every NuGet pin
current, so `ganda nuget outdated` reports 0.

Acceptance:
- `global.json` on 11.0.100 (`rollForward: latestFeature`); CI workflow confirmed unaffected or updated.
- Every pin in the description table at its listed version (plus anything new outdated lists), no
  backward pins, so `ganda nuget outdated` reports 0. When a bump breaks, fix forward or stop and
  report it in `## Notes` (that is how a pin may stay behind). Aspire bumps only if outdated lists it
  or a fix-forward needs it. FastEndpoints GA with the beta-pin comment rewritten so it states the GA
  dependency facts.
- Majors (Blazilla 3, TimeWarp.Build.Tasks 2) read for breaking changes; every forced code change
  listed in Results.
- New analyzer diagnostics fixed at source (no suppressions), each listed in Results.
- `dev build` 0/0, `dev test`, `dev template-smoke --matrix`, Playwright, `dev check-version` pass;
  `ganda repo audit` clean.
- No cross-repo tasks/PRs; upstream defects are reported, not fixed here.

## Checklist

- [x] SDK and Microsoft.* pins on GA; Roslyn 5.12.0; CI updated (EF family held on rc.3, see Notes)
- [x] TimeWarp pins current (Mediator beta.9, Contracts beta.33, SourceGenerators, Jaribu, Nuru, Amuru, Terminal, Build.Tasks 2, Multiavatar)
- [x] Third-party pins current (FastEndpoints 9 GA with comment rewrite, Blazilla 3 with validator fixes, Scalar, OpenTelemetry, icons)
- [ ] `ganda nuget outdated` 0 (6 EF pins blocked on Npgsql 11.0.0 GA, see Notes); build 0/0; tests; template-smoke --matrix; Playwright; check-version; audit clean
- [x] Results list every forced code change and every new diagnostic fixed

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-16)
- Implemented 2026-11-16 (implementer-claude, ganda task work): pins, SDK, CI, lift references,
  gates. Boy-scout: `ganda repo audit --fix --checks memsearch-scaffold,vscode-window-icon`
  refreshed `.githooks/*.cs` and added `.githooks/prepare-commit-msg{,.cs}`.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-16T11:00:22Z
- Review oracle: grok session 01a1e404-05dd-76b3-8f24-4c107a376806 (2026-11-16), effort 2, roster general, round 1, max-turns 120
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1e404-05dd-76b3-8f24-4c107a376806, max-turns 120, artifacts general.md, merged.md — 2026-11-16T11:09:22Z
- Fix pass for review round 1 (implementer-claude, 2026-11-16): M1 fixed. The FastEndpoints pin
  comment in `Directory.Packages.props` and the two OpenApi reference comments in
  `foundation-server.csproj` now state the GA nuspec facts. Goal restatement amended with the
  original's fix-forward/stop clause and the Aspire rule (see Learnings).
- Review oracle round 2: grok session 01a1e417-6866-79f2-af79-626ad1ea39d0 (2026-11-16), effort 1, roster general, max-turns 80
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1e417-6866-79f2-af79-626ad1ea39d0, max-turns 80, artifacts review/round-2/general.md, review/round-2/merged.md, review/disposition.md — 2026-11-16

## Results

### Pins moved

- `global.json` and all 22 test-project `global.json` files: SDK `11.0.100-rc.3.26522.148` → `11.0.100`
  (`rollForward: latestFeature` kept).
- `Directory.Packages.props`: 56 of the 62 rows `ganda nuget outdated` listed, at the versions in
  the description table (`ganda nuget outdated --update --force`, then the EF family put back on
  rc.3; see Notes).
- Not on the original list: `<AspireVersion>` 13.6.4 → **13.6.5**, and the Kubernetes /
  EntityFrameworkCore preview suffix `-preview.1.26523.11` → `-preview.1.26559.7`. Restore failed
  on 13.6.4 with new advisories (NU1904 critical GHSA-4g86-54fm-h9h8 on
  Aspire.Dashboard.Sdk.linux-x64, NU1903 high GHSA-f7vh-h5g3-7jrh on Aspire.Hosting /
  Aspire.Hosting.AppHost). Outdated does not list Aspire because the pins read `$(AspireVersion)`.
  The local Aspire CLI was updated to 13.6.5 to match (`dotnet tool update -g Aspire.Cli`).
- New pin `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.12.0 (Roslyn lift, below).
- Comments rewritten: the FastEndpoints beta-pin comment (now GA), the shared-framework train
  note (11.0.0 / SDK 11.0.100), the four "No 11.x, so the pin is 10.11.0" Extensions comments
  (removed), the Extensions.AI.OpenAI note, the Nuru version in the Mediator note, the Pkcs history
  line, and the Npgsql/EF note (why the EF family is still on rc.3).
- `.config/dotnet-tools.json` `dotnet-ef` stays `11.0.0-rc.3.26522.148`, matching the held EF family.

### CI

`.github/workflows/workflow.yml`: the four `actions/setup-dotnet` steps used
`dotnet-version: '11.0.x'` + `dotnet-quality: 'preview'` with an "RC1 is the go-live SDK" comment.
They now use `global-json-file: global.json`, so CI runs the SDK `global.json` names (the
timewarp-state pattern). `dev workflow` needed no change: it reads `<AspireVersion>`, not the SDK.

### Code changes the bumps forced

1. **Roslyn 5.12 vs EF Design (NU1608, restore).** `Microsoft.EntityFrameworkCore.Design` (rc.3
   and 11.0.0 GA alike) depends on `Microsoft.CodeAnalysis.CSharp.Workspaces` /
   `Workspaces.MSBuild` 5.9.0, whose `Common`/`CSharp` deps are exact. `TimeWarp.SourceGenerators`
   1.0.0-beta.16 declares `Microsoft.CodeAnalysis.CSharp >= 5.12.0` (beta.13 declared 5.9.0, so the
   versions matched before). Fix: `web-infrastructure.csproj` and `web-server.csproj` (the two EF
   Design consumers) add `Microsoft.CodeAnalysis.CSharp.Workspaces` and
   `Microsoft.CodeAnalysis.Workspaces.MSBuild` with `PrivateAssets="all"`, lifting the Workspaces
   family to 5.12.0. No suppression.
2. **Jaribu 1.0.0-beta.16 `TestRunner.TimeProvider` (CS0229).** web-spa-integration-tests has
   `using static TimeWarp.Jaribu.TestRunner`. beta.16 adds a `TimeProvider` member, so a bare
   `TimeProvider.System` is ambiguous. Fixed by qualifying `System.TimeProvider.System` at three
   sites: `components/filter-bar-render-tests.cs` (2) and `infrastructure/static-render-host.cs` (1).
3. **Blazilla 3.0.1 (major).** Read MIGRATION.md (v3.0.1) and v3.0.0 release (#235: native .NET 11
   async validation). Breaking items: `AsyncMode` obsolete, `PendingTask` unused on net11,
   `EditContext.Validate()` obsolete on net11, `ValidateAsync` signature, async field validation.
   Every `<FluentValidator>` site (RoleForm, ProfilePage ×2, AuthenticationPage, AiPage ×3,
   FeedbackListPage) already uses `OnValidSubmit` with no `AsyncMode`, `PendingTask`, or manual
   `Validate`/`ValidateAsync` in the submit handler. TodoItemForm has no validator in markup. That is
   the net11 pattern 3.0 recommends, so **no code change**. Playwright exercises those forms and passed.
4. **TimeWarp.Build.Tasks 2.0.0 (major) → pinned 2.0.1-beta.1.** Release v2.0.0 is "retarget to
   net11.0" (PR #11); TWBT0001 fires only on .NET Framework MSBuild, and this repo builds with the
   dotnet CLI only, so no code change. The pin is 2.0.1-beta.1, not 2.0.0, because
   TimeWarp.Nuru 4.0.0-beta.14 depends on `TimeWarp.Build.Tasks >= 2.0.1-beta.1`, so a 2.0.0 pin is
   NU1605 (downgrade) in the `tools/dev-cli/dev.cs` runfile. `./bin/dev build` does not compile that
   runfile; the first commit's attest `bin-dev` check caught it. v2.0.1-beta.1 only consumes the
   published 2.0.0 (PR #12). The pin comment says to take the next stable at or above it.
   `./bin/dev` was rebuilt (`dotnet run --file tools/dev-cli/dev.cs -- self-install`) on the new
   Nuru/Amuru/Terminal pins with no source change.
5. **FastEndpoints 9.0.0 GA, Mediator beta.9, Contracts beta.33, Nuru beta.14, Amuru beta.5,
   Terminal beta.2, Multiavatar beta.15, OpenTelemetry 1.21, Scalar 2.21, Extensions 11.0.0:** no
   compile or test change.

### New analyzer diagnostics

None. With Roslyn 5.12.0, NetAnalyzers 11.0.100, SourceGenerators beta.16 and Mediator beta.9,
`./bin/dev build` reported 0 warnings. The only compile error was CS0229 above (a name clash, not
an analyzer rule).

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | Build completed successfully, 0 Warning(s), 0 Error(s) (rerun after the Build.Tasks pin fix: same) |
| Test (full, incl. Playwright) | `./bin/dev test -q` | 23 of 23 projects passed (rerun after the Build.Tasks pin fix: 23/23, web-spa-playwright-tests 416.8 s) |
| Template smoke | `./bin/dev template-smoke --matrix` | Template smoke SUCCEEDED; SmokeDefault, SmokeNoApi, SmokeNoGrpc, SmokeNoPostgres, SmokeNoWeb, SmokeNoYarp OK; generated web-jaribu 464/464, api-jaribu 9/9, timewarp-testing 14/14 |
| Version | `./bin/dev check-version` | Source 2.0.0-beta.32 vs NuGet 2.0.0-beta.31: "Version in source is new — safe to release." No bump needed. |
| Outdated | `ganda nuget outdated` | 6 outdated (the EF family; blocked on Npgsql, see Notes). 62 before. |
| Audit | `ganda repo audit` | First commit's attest failed `bin-dev` (NU1605 above); after the fix, rerun: Repository passes. Advisories fixed with `--fix --checks memsearch-scaffold,vscode-window-icon`. One advisory remains: source-file-line-cap on pre-existing generated EF migration files and a done-task research runfile (warning severity; TWA0038 enforces). |

### How to validate

Smoke:
```bash
cat global.json                       # 11.0.100
ganda nuget outdated 2>&1 | grep -- '->'   # only the 6 EF-family rows
./bin/dev build                       # 0 warnings, 0 errors
./bin/dev test -g playwright          # Blazilla 3 forms (roles, profile, auth) still submit
```

Expect: SDK 11.0.100; outdated lists only Microsoft.EntityFrameworkCore{,.Design,.InMemory,.Tools},
Microsoft.AspNetCore.DataProtection.EntityFrameworkCore and
Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (rc.3 → 11.0.0); build 0/0;
Playwright passes.

### Review

Round 1 (2026-11-16), effort 2, general only, raised M1 (bug): the FastEndpoints pin comment and the foundation-server OpenApi comments still described the RC3 range. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

Round 2 (2026-11-16), effort 1, general only, re-reviewed `15da1a135` since round-1 base `d57de9b6a`. M1 is fixed. No new findings. Counts: bug 1 fixed; suggestion 0; nit 0. Open: 0. Disposition: clean. Paths: `review/review-framework.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`. Gates were not re-run; the fix-pass record below covers the comment-only edit.

### Review fix pass

M1 (round 1) fixed. Checked against the local NuGet cache nuspecs (same files the reviewer fetched):
`microsoft.aspnetcore.openapi/11.0.0` depends on `Microsoft.OpenApi` `[3.11.0, 4.0.0)`;
`fastendpoints.openapi/9.0.0` net11.0 depends on `Microsoft.AspNetCore.OpenApi` 11.0.0.

- `Directory.Packages.props`: the FastEndpoints comment now says FastEndpoints.OpenApi 9.0.0
  (net11.0) depends on Microsoft.AspNetCore.OpenApi 11.0.0 (the pin), which depends on
  Microsoft.OpenApi `[3.11.0, 4.0.0)` (3.11.0 is the pin). The "lifts that floor" sentence is gone.
- `source/foundation/foundation-server/foundation-server.csproj`: the two comments over the
  `Microsoft.AspNetCore.OpenApi` and `Microsoft.OpenApi` references no longer say "lift an rc.2
  floor to the RC3 train" / "lift a 3.10.2 floor"; each states the GA dependency and that the pin
  matches it. The direct references stay, as the finding asked.
- No other `RC3`, `rc.2`, or `3.10.2` text remains in `Directory.Packages.props`,
  `Directory.Build.props`, `msbuild/`, or foundation-server.

Fix-pass gates (comment-only edits; recorded once here):

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | Build succeeded, 0 Warning(s), 0 Error(s) |
| Test (full, incl. Playwright) | `./bin/dev test -q` | 23 of 23 projects passed (web-spa-playwright-tests 455.5 s) |
| Audit | `ganda repo audit` | Repository passes; same single advisory as before (source-file-line-cap, warning, pre-existing generated EF migrations and a done-task runfile) |

## Review

- Round 1: Blocked — open M1
- Round 1 fix pass: M1 fixed (comments only; see `### Review fix pass` under Results)
- Round 2: Done
## Notes

Steve 2026-11-16: "review mediator and state, as both have had many commits and this repo is
behind" → survey found no library-surface changes since the current pins, only the toolchain
move to .NET 11 GA and Roslyn 5.12.0. Task 483 (`[ContractState]`) stays blocked on its own
design question; nothing here changes it. Architecture 2.0.0-beta.32 releases after this lands.

**EF family held on rc.3 (task 492, 2026-11-16).** `Npgsql.EntityFrameworkCore.PostgreSQL`
11.0.0-rc.3 is the newest Npgsql provider on nuget.org (no 11.0.0 GA), and its nuspec pins
`Microsoft.EntityFrameworkCore` and `.Relational` to exactly `[11.0.0-rc.3.26522.148]`. Moving the
EF family to 11.0.0 was tried and fails restore with NU1107. So six pins stay on rc.3 and
`ganda nuget outdated` reports 6, not 0. This is not a backward pin; those packages are unchanged.
Follow-up: when Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0 ships, move it, the six EF pins and
`dotnet-ef` in `.config/dotnet-tools.json` together. The Roslyn lift references in web-infrastructure
and web-server stay until EF Design itself takes Roslyn ≥ 5.12 (11.0.0 GA still takes 5.9.0).

## Learnings

Misread: `## Goal (implementer restatement)` drops the original requirement's escape hatch ("if a bump breaks, fix forward or stop and report in `## Notes`") and states success as every table pin at the listed version with `ganda nuget outdated` at 0. The implementation followed the original task: the EF family stays on rc.3 because `Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0-rc.3 exact-pins `Microsoft.EntityFrameworkCore` and `.Relational` at `[11.0.0-rc.3.26522.148]`, and that stop is recorded in Notes. The nuget.org index has no 11.0.0 of that provider. That hold is not a defect. The restatement also omits "bump Aspire only if outdated lists it"; the 13.6.5 bump is the original's fix-forward path after restore advisories, and it is recorded.

Round 2 (2026-11-16): the acceptance bullet now includes that escape hatch. It also says Aspire bumps only if outdated lists it or a fix-forward needs it. The task's Aspire bullet says bump only if outdated lists it, and it says the Aspire CLI must agree with `<AspireVersion>`. The restatement still does not mention the CLI. Results records the CLI update to 13.6.5, and the 13.6.5 pin bump stays the fix-forward already described above. Neither gap is a defect in the pin change.
