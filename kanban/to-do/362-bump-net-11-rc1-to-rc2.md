# Bump .NET 11 RC1 to RC2

## Description

.NET 11 RC2 shipped 2026-10-13 (go-live). Move the repo from RC1 to RC2. The RC2 SDK is installed on Windows, WSL and the box.

## Requirements

- `global.json` sdk: `11.0.100-rc.1.26425.128` -> `11.0.100-rc.2.26504.105`
- `Directory.Packages.props`: every `11.0.0-rc.1.26425.128` -> `11.0.0-rc.2.26504.105`
- Review ASP.NET Core and EF Core RC2 breaking-change notes: https://github.com/dotnet/core/tree/main/release-notes/11.0

## Checklist

- [ ] Update global.json SDK version
- [ ] Update all RC1 package versions in Directory.Packages.props
- [ ] Review ASP.NET Core / EF Core RC2 breaking changes and fix any fallout
- [ ] Build succeeds
- [ ] Tests green
- [ ] Blazor WASM dev server runs

## Session

- Created: 2026-10-14

## Notes

- PRs #494 and #495 both pin System.Security.Cryptography.Pkcs 9.0.21 in Directory.Packages.props; coordinate with them to avoid conflicts.
