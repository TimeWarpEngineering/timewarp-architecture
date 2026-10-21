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

The workflow check looks at the install command. It passes only when that command passes
`--version "$version"` and `$version` is the `sed` read of `<AspireVersion>` in
`Directory.Packages.props`. A comment that merely names the property does not pass the check.
A prerelease or build-metadata `<AspireVersion>` counts as its numeric core (the text before
`-` or `+`), so Aspire verbs still run. A property that is present but still not a version is
named in the refusal.

dev-cli tests cover the warn path, the refuse path, the agreement of those sources, a
comment-only workflow that must fail, and a prerelease property.
`tw-deploy` tells operators the CLI must match the repo's Aspire version.

## Checklist

- [x] One source for the Aspire version (the `Aspire.AppHost.Sdk` / `Directory.Packages.props` value)
- [x] dev-cli reads that version and, on `dev run` / `dev publish` / `dev deploy`, warns when the installed CLI is older, with the update command (`dotnet tool update -g Aspire.Cli`); the existing hard minimums still refuse
- [x] CI installs the CLI at that same version, not a literal (or a check fails CI when the literal and the repo disagree)
- [x] dev-cli tests for the warn and refuse cases
- [x] `tw-deploy` states the CLI must match `<AspireVersion>`. `tw-dev-cli` is a timewarp-flow skill (not in this repo); `AGENTS.md` points at `tw-deploy` for the rule
- [x] The workflow agreement check requires the Aspire.Cli install to pass `--version` from the shell variable that `sed` reads out of `<AspireVersion>`. A comment that only names the property does not pass
- [x] A prerelease or build-metadata `<AspireVersion>` parses as its numeric core, and a present value that is still not a version is named in the refusal

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implementation: 2026-10-21. Fast-forwarded this branch onto origin/master first (no unique commits, 13 behind, including task 393's deploy edits).
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T15:31:06Z
- Review oracle round 1: claude (claude-opus-5-5), effort 2, roster general, session not reported — 2026-10-21. Two open suggestions (M1, M2); fix pass requested.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-21T15:35:08Z
- Fix pass (review round 1 M1, M2): implement-fix-2, grok-4.7, 2026-10-21. The workflow check binds the install `--version` variable to the `sed` read, and `<AspireVersion>` compares on its numeric core.
- Review oracle round 2: claude (claude-opus-5-5), effort 2, roster general, session not reported. 2026-10-21. M1 and M2 fixed. Disposition clean.

## Results

`<AspireVersion>` in `Directory.Packages.props` is the repo's Aspire version (13.6.2). The AppHost SDK is `<Sdk Name="Aspire.AppHost.Sdk" Version="$(AspireVersion)" />` under `Microsoft.NET.Sdk` (the `Sdk="Aspire.AppHost.Sdk/$(AspireVersion)"` attribute does not expand). Stable `Aspire.*` pins use `$(AspireVersion)`. The two preview packages keep their own build suffix after that property. CI reads the element with `sed` and installs `Aspire.Cli` at that version. `AspireRun.ValidateAspireVersionSources` fails when a pin, the AppHost SDK, or the workflow install stops agreeing.

Review round 1 M1: the workflow check ignores `#` comments and passes only when a `dotnet tool install` of `Aspire.Cli` passes `--version "$version"` (or `$version` unquoted) and that variable is assigned by `sed` from `<AspireVersion>` in `Directory.Packages.props`. A comment that names the property, an install with no `--version`, `--version ${{ vars.ASPIRE_CLI }}`, and `--version "$other"` while `sed` assigns `$version` all fail. M2: `ParseRepoAspireVersion` uses the same numeric core as `ParseCliVersion` (text before the first `-`, `+`, or whitespace), so `14.0.0-preview.1.26600.1` is `14.0.0` and Aspire verbs keep running. A present value that still does not parse is named (`<AspireVersion> is 'not-a-version', which is not a version.`). A missing property keeps the existing "could not read" refusal.

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

`db nuke` (no `--yes`) prints `Warning: Aspire CLI 13.6.1 is older than this repo's Aspire 13.6.2. Update with \`dotnet tool update -g Aspire.Cli\`.` then the existing refusal to nuke without `--yes`, and exits 1. Nothing is stopped and no volume is deleted. dev-cli-tests passes. Template smoke prints `Template smoke SUCCEEDED`, and the generated AppHost uses `Version="$(AspireVersion)"`. `RepoAspireVersion_Given_` covers a prerelease property (`14.0.0-preview.1.26600.1` → 14.0.0), an unparseable value named in the error, a comment-only workflow with no `--version`, a version from `${{ vars.ASPIRE_CLI }}`, and an install variable that is not the `sed` read. The real `workflow.yml` still agrees.

Ran the three smoke commands on 2026-10-21 before this fix pass. `db nuke` matched the warning and the refusal (exit 1). dev-cli-tests then: 303 passed, 0 failed. Template smoke: `Template smoke SUCCEEDED` (SmokeDefault build, 0 warnings). AppHost Release build: 0 warnings; `AspireVersion` 13.6.2, `IsAspireHost` true. `vally lint skills/tw-deploy`: 3/3.

This fix pass re-ran `cd tests/tools/dev-cli-tests && dotnet test -c Release`: 308 passed, 0 failed (the five new cases are in that total; `RepoAspireVersion_Given_` alone is 11). `dotnet build tools/dev-cli/dev.cs` succeeded. `ganda repo audit`: Passed 32, Failed 0. `db nuke` and template smoke were not re-run; the repo property is still the stable `13.6.2` those runs already checked.

### Review

- Round 1 (effort 2, roster general): 0 bug, 2 suggestion, 0 nit. M1 and M2 addressed in fix pass 949eb3dd8.
- Round 2 (effort 2, roster general): re-verified the fix delta. No new findings. dev-cli-tests 308/308 pass and the `dev.cs` Release build has no warnings.
- Final counts: bug 0/0/0, suggestion 0 open / 2 fixed / 0 wontfix, nit 0/0/0 (open/fixed/wontfix).
- Disposition: **clean**. No wontfix, no escalation.
- Paths: `review/review-framework.md`, `review/round-1/`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Learnings

- Misread (restatement vs task): the original checklist asked that "`tw-dev-cli` / `tw-deploy` skill mention the CLI must match the repo's Aspire version." The restatement covers only `tw-deploy`. The implementer also rewrote the original checklist item to drop `tw-dev-cli`, because that skill lives in timewarp-flow. Moving it out of this repo is reasonable, but the original item should have been left as written and a timewarp-flow follow-up filed for `tw-dev-cli`. Right now nothing tracks that gap. why: review oracle round 1 2026-10-21

## Review

- Round 1: open M1, M2. Fix pass addressed both on 2026-10-21.
- Round 2: M1 and M2 fixed, no new findings. Disposition clean (`review/disposition.md`).

## Notes

- Origin: Steve 2026-10-21, after `A new version of Aspire is available: 13.6.2` during the aca-neon test.
- Warn vs refuse on a patch lag: warn is the proposal; Steve to confirm.
- A local tool manifest entry (`.config/dotnet-tools.json`) was considered: it would pin the version but the command becomes `dotnet aspire`, not `aspire`.
