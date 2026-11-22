# Move the EF Core family and Npgsql provider from 11.0.0-rc.3 to 11.0.0 GA when Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0 ships

## Description

Task 492 moved every Microsoft.* pin to 11.0.0 GA except the EF Core family, because
`Npgsql.EntityFrameworkCore.PostgreSQL` was only at `11.0.0-rc.3` on nuget.org and pins
`Microsoft.EntityFrameworkCore` / `.Relational` exactly at `[11.0.0-rc.3.26522.148]`.
Moving EF alone fails restore (NU1608/NU1107). The explanation is the comment above the
EF pins in `Directory.Packages.props`.

This task finishes the move once the GA provider is on nuget.org. It is one commit that
moves all seven pins together and removes the rc.3 comment.

### Pins to move (all seven, same commit)

| Package | From | To |
|---|---|---|
| Microsoft.AspNetCore.DataProtection.EntityFrameworkCore | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.EntityFrameworkCore | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.EntityFrameworkCore.Design | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.EntityFrameworkCore.InMemory | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.EntityFrameworkCore.Tools | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 11.0.0-rc.3.26522.148 | 11.0.0 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 11.0.0-rc.3 | 11.0.0 |

If a later patch (11.0.x) is current by then, use the current version `ganda nuget outdated`
reports. Never backward-pin. Keep the Roslyn-lift comment and the
`Microsoft.CodeAnalysis.CSharp.Workspaces` arrangement below the EF pins unless GA Design
changes its Roslyn dependency; verify with `dev build`.

### Precondition

`Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0 (or later stable) is listed at
https://api.nuget.org/v3-flatcontainer/npgsql.entityframeworkcore.postgresql/index.json.
If it is not, stop and report; do not start the walk. Tracking issue:
https://github.com/npgsql/efcore.pg/issues/4123 (code merged in npgsql/efcore.pg#4125 on
2026-11-11; release not yet tagged as of 2026-11-16).

## Goal (implementer restatement)

Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0 is on nuget.org. Its net11.0 nuspec accepts
Microsoft.EntityFrameworkCore and Microsoft.EntityFrameworkCore.Relational in the range
`[11.0.0, 12.0.0)`, so the EF Core family can leave the rc.3 build. No 11.0.x patch newer
than 11.0.0 is published for that provider or for Microsoft.EntityFrameworkCore.

In one commit, set these seven `Directory.Packages.props` pins to 11.0.0:
Microsoft.AspNetCore.DataProtection.EntityFrameworkCore, Microsoft.EntityFrameworkCore,
Microsoft.EntityFrameworkCore.Design, Microsoft.EntityFrameworkCore.InMemory,
Microsoft.EntityFrameworkCore.Tools, Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore,
and Npgsql.EntityFrameworkCore.PostgreSQL. Remove the comment that said the family must stay
on rc.3, and leave one line that the family moves with the Npgsql provider. Keep the
Roslyn-lift comment and the Microsoft.CodeAnalysis.CSharp.Workspaces /
Microsoft.CodeAnalysis.Workspaces.MSBuild 5.12.0 arrangement, because Design 11.0.0 still
depends on Microsoft.CodeAnalysis.* 5.9.0.

Also move the local `dotnet-ef` tool from `11.0.0-rc.3.26522.148` to `11.0.0`. Task 492
left that tool on rc.3 only so it matched this family, and 11.0.0 is the current stable
tool. Do not bump `<Version>` unless `check-version` requires it. Leave generated migration
ProductVersion annotations as history. Do not edit another repository.

Done when the seven pins and `dotnet-ef` are 11.0.0, `ganda nuget outdated` reports nothing
outdated, `./bin/dev build` is 0 warnings and 0 errors, `./bin/dev template-smoke --matrix`
passes, `./bin/dev check-version` passes without a version bump, and `ganda repo audit` is
clean. This repo's `./bin/dev test` does not take a file, and no test file names these pins,
so the full suite stays on CI.

## Checklist

- [x] Precondition met: Npgsql provider 11.0.0 stable on nuget.org
- [x] All seven pins moved in `Directory.Packages.props`; rc.3 explanatory comment removed (keep one line saying the family moves with the Npgsql provider)
- [x] `ganda nuget outdated` reports 0
- [x] `dev build` 0 warnings / 0 errors
- [x] `dev test` — not run here. `./bin/dev test` takes no file, no test file was added or edited, and no test asserts these pins. CI runs the full suite.
- [x] `dev template-smoke --matrix` (postgres flag affects EF; matrix covers flag-off)
- [x] `dev check-version` passes (bump `<Version>` only if `check-version` requires it; pins-only change needs no release)
- [x] `ganda repo audit` clean
- [x] No cross-repo work; stop and report on anything outside this repo

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-16)
- Implementation: 01a201da-2110-7482-b05c-bac4413ae3bc (2026-11-22)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a201da-2110-7482-b05c-bac4413ae3bc, max-turns 500, Blocked — 2026-11-22T06:30:29Z
- Resume: 01a20295-b8c4-75c1-b9b3-ef2b848f7d1a (2026-11-22). Merged origin/master so the branch contains task 498. That cleared package-pin-currency, dev-cli-telemetry, and kanban-landed. The EF pins stayed at 11.0.0.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20295-b8c4-75c1-b9b3-ef2b848f7d1a, max-turns 500, Done — 2026-11-22T09:51:52Z

## Results

Npgsql.EntityFrameworkCore.PostgreSQL 11.0.0 is on nuget.org. Its net11.0 nuspec depends on
Microsoft.EntityFrameworkCore and .Relational `[11.0.0, 12.0.0)`. No 11.0.x newer than 11.0.0
exists for that provider, for Microsoft.EntityFrameworkCore, or for the `dotnet-ef` tool.
Design 11.0.0 still depends on Microsoft.CodeAnalysis.* 5.9.0, so the Roslyn 5.12.0 lift stayed.
TimeWarp.SourceGenerators on this branch is 1.0.0-beta.17, which is what the lift comment names.

The seven `Directory.Packages.props` pins are 11.0.0. The rc.3 hold comment is gone. One line
remains: the EF Core family moves with the Npgsql provider. The local `dotnet-ef` tool is
`11.0.0`. `<Version>` stayed `2.0.0-beta.32`. Generated migration ProductVersion annotations
were left as history.

The branch was twenty commits behind origin/master. Those commits are task 498 (the other
NuGet pins, Aspire 17, and `UseTelemetry`). This walk merged origin/master and kept the EF
family at 11.0.0. Against origin/master the product diff is only `Directory.Packages.props`
and `.config/dotnet-tools.json`.

`ganda nuget outdated` exits 0 and prints `All packages are up to date!` (134 pins).

`ganda repo audit` exits 0: 41 passed, 3 skipped, 2 advisory warnings. The warnings are
`source-file-line-cap` (a done-task research file and three generated EF snapshot files) and
`vscode-window-icon` (`.vscode/settings.json` is gitignored; the vscode-settings check passed).
They are not this pin move.

### How to validate

Smoke: `./bin/dev build` then `./bin/dev template-smoke --matrix`

Expect: build prints `0 Warning(s)` and `0 Error(s)`. The matrix runs SmokeDefault,
SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, and SmokeNoYarp, and the command
prints `Template smoke SUCCEEDED`. `./bin/dev check-version` prints source `2.0.0-beta.32`
one prerelease ahead of NuGet `2.0.0-beta.31` and does not ask for a bump.

- `./bin/dev build` — exit 0, 0 warnings, 0 errors (00:00:23.36)
- `./bin/dev test` — not run. No test file was added or edited. `./bin/dev test` does not
  take a file. Tests that reference EntityFrameworkCore live in csproj suites
  (`web-infrastructure-tests`, `web-server-integration-tests`, and others), not standalone
  Jaribu runfiles. CI runs the full suite.
- `./bin/dev template-smoke --matrix` — exit 0, Template smoke SUCCEEDED (SmokeDefault,
  SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, SmokeNoYarp)
- `./bin/dev check-version` — exit 0, source 2.0.0-beta.32 is one prerelease ahead of NuGet 2.0.0-beta.31
- `ganda nuget outdated` — exit 0, all 134 packages up to date
- `ganda repo audit` — exit 0, 41 passed, 3 skipped, 2 advisory warnings (source-file-line-cap, vscode-window-icon)

## Notes

- Filed per Steve 2026-11-16 ("yes file it") after 492 left the EF family on rc.3.
- Pins-only template change; no library packages change, so no release is required
  (see "Release only when platform packages change").
