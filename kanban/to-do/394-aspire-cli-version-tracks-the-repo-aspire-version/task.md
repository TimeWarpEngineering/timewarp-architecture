# Aspire CLI version tracks the repo Aspire version

## Description

The repo builds against Aspire 13.6.2 (`Aspire.AppHost.Sdk` and the Aspire packages), and CI
installs `Aspire.Cli --version 13.6.2` (`.github/workflows/workflow.yml`). A developer's global
CLI can lag: on 2026-10-21 Steve's was 13.6.1 and only Aspire's own banner said so. dev-cli's
guard (`AspireCli.ValidateVersionAsync`, `AspireRun.ValidateCliVersion`) checks a hard-coded
minimum (`new(13, 6)`), so it never notices a CLI older than the repo.

Three places carry the Aspire version by hand. When two things must agree, generate one from the
other or add a build check.

## Goal (implementer restatement)

One Aspire version lives in `Directory.Packages.props` as `<AspireVersion>`. The AppHost SDK,
the stable and preview `Aspire.*` package pins, and the CI `Aspire.Cli` install all take that
value. They do not each carry their own copy of the number.

`dev run`, `dev publish`, and `dev deploy` read that version from the repo. When the installed
`aspire` CLI is older, they print a warning and the update command `dotnet tool update -g Aspire.Cli`,
then continue. A CLI below an existing hard minimum (13.6 for `--launch-profile`, publish, deploy,
and `db nuke`) is still refused. A CLI that is the same or newer is left alone.

CI installs `Aspire.Cli` by reading `<AspireVersion>` rather than a literal version. A check fails
when the AppHost SDK, an Aspire package pin, or the workflow install stops agreeing with that property.

dev-cli tests cover the warn path, the refuse path, and the agreement of those sources.
`tw-deploy` tells operators the CLI must match the repo's Aspire version.

## Checklist

- [x] One source for the Aspire version (the `Aspire.AppHost.Sdk` / `Directory.Packages.props` value)
- [x] dev-cli reads that version and, on `dev run` / `dev publish` / `dev deploy`, warns when the installed CLI is older, with the update command (`dotnet tool update -g Aspire.Cli`); the existing hard minimums still refuse
- [x] CI installs the CLI at that same version, not a literal (or a check fails CI when the literal and the repo disagree)
- [x] dev-cli tests for the warn and refuse cases
- [x] `tw-deploy` states the CLI must match `<AspireVersion>`. `tw-dev-cli` is a timewarp-flow skill (not in this repo); `AGENTS.md` points at `tw-deploy` for the rule

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implementation: 2026-10-21. Fast-forwarded this branch onto origin/master first (no unique commits, 13 behind, including task 393's deploy edits).

## Results

`<AspireVersion>` in `Directory.Packages.props` is the repo's Aspire version (13.6.2). The AppHost SDK is `<Sdk Name="Aspire.AppHost.Sdk" Version="$(AspireVersion)" />` under `Microsoft.NET.Sdk` (the `Sdk="Aspire.AppHost.Sdk/$(AspireVersion)"` attribute does not expand). Stable `Aspire.*` pins use `$(AspireVersion)`. The two preview packages keep their own build suffix after that property. CI reads the element with `sed` and installs `Aspire.Cli` at that version. `AspireRun.ValidateAspireVersionSources` fails when a pin, the AppHost SDK, or the workflow install stops agreeing.

`dev run`, `dev publish`, and `dev deploy` warn when the installed CLI is older and print `dotnet tool update -g Aspire.Cli`, then continue. A CLI below 13.6 is still refused where that minimum already applied (`dev run --launch-profile`, publish, deploy, and `db nuke`). `dev deploy`'s publish step does not print the warning a second time. A missing `<AspireVersion>` is a refusal. The same or a newer CLI is left alone.

`tw-deploy` has an "Aspire CLI version" section (496 lines, under the 500-line skill limit). `tw-dev-cli` was not edited: it lives on timewarp-flow master, and this walk does not implement there.

`./bin/dev` is the AOT binary and does not include this warning until the next self-install. The checks below used `dotnet run tools/dev-cli/dev.cs`.

### How to validate

Smoke:

```bash
dotnet run tools/dev-cli/dev.cs -c Release -- db nuke
cd tests/tools/dev-cli-tests && dotnet test -c Release
dotnet run tools/dev-cli/dev.cs -c Release -- template-smoke
```

Expect:

`db nuke` (no `--yes`) prints `Warning: Aspire CLI 13.6.1 is older than this repo's Aspire 13.6.2. Update with \`dotnet tool update -g Aspire.Cli\`.` then the existing refusal to nuke without `--yes`, and exits 1. Nothing is stopped and no volume is deleted. dev-cli-tests passes (303). Template smoke prints `Template smoke SUCCEEDED`, and the generated AppHost uses `Version="$(AspireVersion)"`.

Ran those three on 2026-10-21. `db nuke` matched the warning and the refusal (exit 1). dev-cli-tests: 303 passed, 0 failed. Template smoke: `Template smoke SUCCEEDED` (SmokeDefault build, 0 warnings). AppHost Release build: 0 warnings; `AspireVersion` 13.6.2, `IsAspireHost` true. `vally lint skills/tw-deploy`: 3/3. `ganda repo audit`: Passed 32, Failed 0.

## Notes

- Origin: Steve 2026-10-21, after `A new version of Aspire is available: 13.6.2` during the aca-neon test.
- Warn vs refuse on a patch lag: warn is the proposal; Steve to confirm.
- A local tool manifest entry (`.config/dotnet-tools.json`) was considered: it would pin the version but the command becomes `dotnet aspire`, not `aspire`.
