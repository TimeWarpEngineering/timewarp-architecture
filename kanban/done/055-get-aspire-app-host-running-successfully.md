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

## Follow-up Work

After the AppHost launched, the dashboard initially showed no resources because the AppHost feature preprocessor symbols were not defined. Added the AppHost feature constants for `api`, `web`, and `grpc`, then enabled `yarp` after moving to the first-party Aspire YARP integration.

Resolved Linux launch-profile discovery by moving service `launchSettings.json` files from lowercase `properties/` folders to canonical `Properties/` folders. This allowed Aspire to discover the project launch profiles and avoid the default Kestrel port collision on `5000`.

Fixed API endpoint generation by exposing `EnableApiEndpointGeneration` to source generators with `CompilerVisibleProperty` and correcting the Roslyn metadata lookup for the generic `BaseFastEndpoint` type. The API now registers the generated WeatherForecast FastEndpoint.

Configured OpenCode to use Aspire MCP through `aspire agent mcp`, enabling direct resource status/log inspection. Verified via MCP that `api`, `grpc`, `web`, and `ingress` are all running and healthy.

Replaced the obsolete `Aspirant.Hosting.Yarp` AppHost integration with first-party `Aspire.Hosting.Yarp`, rewrote YARP routing using code-based `WithConfiguration`, and removed the old custom YARP project reference from the AppHost. Kubernetes ingress replacement was intentionally deferred for later evaluation.

Related commits:

- `ae43f3a2` `fix: restore Aspire project startup`
- `cb89e43f` `fix: use Aspire YARP gateway`
