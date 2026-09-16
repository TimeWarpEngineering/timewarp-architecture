# Bump version and platform pins to 2.0.0-beta.19 for the Entra iss-claim fix

## Description

This bump ships task 221's Entra `iss` claim fix as 2.0.0-beta.19. The OIDC default ClaimAction was deleting the `iss` claim, which broke real Entra sign-in with a 400 "Invalid Entra token" error. Task 221 also included a return-URL fix and diagnostics improvements. This release is needed so the "crunchit" project can consume the fix. 2.0.0-beta.18 is the affected version.

## Checklist

- [ ] Bump version and platform pins
- [ ] Verify build + check-version
- [ ] Open PR

## Session

Created: cockpit (2026-09-16)
Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Results

### How to validate

- `./bin/dev check-version` or `dotnet run tools/dev-cli/dev.cs -- check-version` should report source version 2.0.0-beta.19 is newer than the published NuGet version 2.0.0-beta.18
- `dev build` (or `dotnet run tools/dev-cli/dev.cs -- build`) should report 0 warnings / 0 errors
