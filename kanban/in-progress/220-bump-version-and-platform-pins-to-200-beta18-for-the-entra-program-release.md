# Bump version and platform pins to 2.0.0-beta.18 for the Entra program release

## Description

Bump version and platform pins from 2.0.0-beta.17 to 2.0.0-beta.18 to release the work completed in task 219 (Entra program work: subtasks 219-001 through 219-006) for consumption by the crunchit project. This includes PublicOrigin with secure OIDC cookies, dev entra CLI, and site settings store with Entra sign-in policy seam. Platform pins must equal the `<Version>` element per task-124 policy and bump in the same commit.

## Checklist

- [ ] Bump `<Version>` and all `PackageVersion` pins to 2.0.0-beta.18 in source/Directory.Build.props, timewarp-templates/Directory.Build.props, and Directory.Packages.props
- [ ] Run `dev build` clean and `dev check-version` reports source newer than NuGet
- [ ] Open PR

## Session

- Created: 353415 (2026-09-15)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Results

### How to validate

- Run `./bin/dev check-version` or `dotnet run tools/dev-cli/dev.cs -- check-version` and confirm it reports source version 2.0.0-beta.18 is newer than the published NuGet version 2.0.0-beta.17
- Run `dev build` and confirm it completes with 0 warnings / 0 errors
