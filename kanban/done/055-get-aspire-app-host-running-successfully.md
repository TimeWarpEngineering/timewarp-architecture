# Get Aspire App Host running successfully

## Description

The Aspire App Host project fails to run. The `aspire run` command against `source/ContainerApps/Aspire/Aspire-App-Host/Aspire-App-Host.csproj` produces build errors due to an unsupported Aspire.Hosting package version and other issues that need to be resolved.

## Checklist

- [x] Update Aspire.Hosting NuGet package from 9.0.0 to the latest supported version
- [x] Update Aspire CLI to the latest version (13.4.2 available)
- [x] Resolve developer certificate trust issue (`PartiallyFailedToTrustTheCertificate`)
- [x] Verify the Aspire App Host project builds successfully
- [x] Verify `aspire run` starts the app host without errors

## Notes

**Error output from `aspire run`:**
```
💾 Created settings file at 'aspire.config.json'.
⚠️ Developer certificates may not be fully trusted (trust exit code was: PartiallyFailedToTrustTheCertificate).
❌ The Aspire.Hosting package version 9.0.0 is not supported. Please update to the latest version.
❌ The project could not be built. See logs at /home/steve/.aspire/logs/cli_20260607T021004_d299d8b8.log
```

**Key issues:**
1. **Aspire.Hosting 9.0.0 not supported** — The project references an outdated version of the Aspire.Hosting package. Needs upgrade to the latest compatible version.
2. **Aspire CLI outdated** — Version 13.4.2 is available; current version needs updating via `aspire update`.
3. **Dev certs not fully trusted** — May need `dotnet dev-certs https --trust` or similar.

**Project path:** `source/ContainerApps/Aspire/Aspire-App-Host/Aspire-App-Host.csproj`

## Outcome

Updated Aspire project packages and CLI to 13.4.2, aligned incompatible .NET 10 package versions, fixed the obsolete Aspire command result API usage, and verified `dev build`, `aspire doctor`, and `aspire run`.
