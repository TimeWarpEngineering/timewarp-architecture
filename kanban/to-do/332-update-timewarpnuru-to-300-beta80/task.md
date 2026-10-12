# Update TimeWarp.Nuru to 3.0.0-beta.80

## Description

`ganda repo attest` refuses to sign master because its outdated-package audit flags TimeWarp.Nuru (and TimeWarp.Nuru.DevCli) at 3.0.0-beta.79 while 3.0.0-beta.80 is published. Task 331's publish commit 487bfde is unsigned as a result. Bump both pins in `Directory.Packages.props` to 3.0.0-beta.80 so the audit clears and master can be attested. Requested by Steven 2026-10-13. Precedent: task 283 (beta.79 bump).

## Checklist

- [x] Bump `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` to 3.0.0-beta.80 in `Directory.Packages.props`
- [x] Fix any breaking changes from beta.80 (check Nuru release notes)
- [x] `dev build` passes
- [x] `ganda repo audit` reports no outdated Nuru packages

## Session

- Created: 1202770 (2026-10-12)
- Implementer: grok session 01a13156-d77d-7993-9a11-1df6c34f5e00 (2026-10-13)

## Notes

Scope is the Nuru bump only. Do not change other packages unless beta.80 requires it.

beta.80 ([release](https://github.com/TimeWarpEngineering/timewarp-nuru/releases/tag/v3.0.0-beta.80)) does not require other pin moves. Its nuspec still depends on `TimeWarp.Amuru` 2.0.0-beta.2, `TimeWarp.Mediator.Contracts` / `TimeWarp.Mediator.Generators` 14.0.0-beta.4, `TimeWarp.Terminal` 1.0.2, and `TimeWarp.Build.Tasks` 1.0.0, which this repo already pins.

Consumer-visible delta, none of which needed a source edit here:

- `dev release` (DevCli content compiled into the dev CLI) adds `--prerelease` to `gh release create` when `<Version>` has a SemVer prerelease suffix. This repo is `2.0.0-beta.20`, so the next release marks the GitHub Release as a prerelease, matching NuGet.
- The package ships `build/TimeWarp.Nuru.props` (and `buildTransitive`) that append `TimeWarp.Nuru.Generated` to `InterceptorsNamespaces`. Root and tool `Directory.Build.props` already append the same namespace. Both CLIs built.
- `NURU_H002` no longer treats a nested lambda parameter as a closure. No call-site change.
- `NuruApp.RunReplAsync` returns `Task<int>`. Nothing in this repo calls it. Bash completion and redirected-stdin REPL fixes are inside the package.

## Learnings

- Nuru 3.0.0-beta.80 is a dogfood bump on the same Amuru 2.0.0-beta.2 and Mediator 14.0.0-beta.4 floor as beta.79. The audit failure was the pin, not an API migration.

## Results

Pinned `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` from 3.0.0-beta.79 to 3.0.0-beta.80 in `Directory.Packages.props`. No other packages changed. No product source edits: the beta.80 release notes and the published nuspec do not break the dev CLI or the agent-identity CLI.

**Files:** `Directory.Packages.props`.

**Gates:**

- `dotnet build tools/dev-cli/dev.cs -c Release` succeeded.
- `dotnet build tools/agent-identity-cli/agent.cs -c Release` succeeded.
- `./bin/dev build` (Release solution): 0 Warning(s), 0 Error(s), elapsed 00:14:27.
- `dotnet run tools/dev-cli/dev.cs -- self-install`, then `./bin/dev --capabilities` (30 endpoints) and `./bin/dev check-version` ("Version in source is new — safe to release."). `bin/dev` is gitignored.
- `ganda repo audit`: Passed 31, Failed 0. `nuru` PASS — "TimeWarp.Nuru is up to date".

### How to validate

**Smoke**

```bash
grep -n 'PackageVersion Include="TimeWarp.Nuru' Directory.Packages.props
dotnet build tools/dev-cli/dev.cs -c Release
dotnet build tools/agent-identity-cli/agent.cs -c Release
./bin/dev build
ganda repo audit
```

**Expect**

- Both `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are `3.0.0-beta.80`.
- The two CLI builds succeed.
- `./bin/dev build` ends with `0 Warning(s)` and `0 Error(s)`.
- `ganda repo audit` reports Failed: 0 and the `nuru` row says `TimeWarp.Nuru is up to date`.

**Automated gate**

```bash
./bin/dev build
ganda repo audit
```

**Not in scope:** signing master with `ganda repo attest` (that runs after this pin is on master), and cutting a release. The next `dev release` will pass `--prerelease` because the version suffix is `-beta`.
