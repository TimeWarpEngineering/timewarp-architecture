# Aspire CLI version tracks the repo Aspire version

## Description

The repo builds against Aspire 13.6.2 (`Aspire.AppHost.Sdk` and the Aspire packages), and CI
installs `Aspire.Cli --version 13.6.2` (`.github/workflows/workflow.yml`). A developer's global
CLI can lag: on 2026-10-21 Steve's was 13.6.1 and only Aspire's own banner said so. dev-cli's
guard (`AspireCli.ValidateVersionAsync`, `AspireRun.ValidateCliVersion`) checks a hard-coded
minimum (`new(13, 6)`), so it never notices a CLI older than the repo.

Three places carry the Aspire version by hand. When two things must agree, generate one from the
other or add a build check.

## Checklist

- [ ] One source for the Aspire version (the `Aspire.AppHost.Sdk` / `Directory.Packages.props` value)
- [ ] dev-cli reads that version and, on `dev run` / `dev publish` / `dev deploy`, warns when the installed CLI is older, with the update command (`dotnet tool update -g Aspire.Cli`); the existing hard minimums still refuse
- [ ] CI installs the CLI at that same version, not a literal (or a check fails CI when the literal and the repo disagree)
- [ ] dev-cli tests for the warn and refuse cases
- [ ] `tw-dev-cli` / `tw-deploy` skill mention the CLI must match the repo's Aspire version

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Origin: Steve 2026-10-21, after `A new version of Aspire is available: 13.6.2` during the aca-neon test.
- Warn vs refuse on a patch lag: warn is the proposal; Steve to confirm.
- A local tool manifest entry (`.config/dotnet-tools.json`) was considered: it would pin the version but the command becomes `dotnet aspire`, not `aspire`.
