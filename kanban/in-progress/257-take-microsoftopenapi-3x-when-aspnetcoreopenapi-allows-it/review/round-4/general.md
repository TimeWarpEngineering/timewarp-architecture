# Round 4 — general
**Date:** 2026-11-01
**Scope reviewed:** 15fef86b0 (`source/container-apps/web/projects/web-server/web-server.csproj`), plus the
kanban Results/Session edits in 6ebb7e6ee, 3e9d65272, 8c54c230f.

## Summary

The PR #621 publish job failed while generating the EF migration bundle. The fix sets
`PrivateAssets=all` on web-server's Oakton reference, so Oakton's `Microsoft.Extensions.Hosting
[6.0.0, 10.0.0)` ceiling no longer reaches the generated `Microsoft.NET.Sdk` bundle project. That
project also takes the `Microsoft.Extensions.Hosting 11.0.0-rc.3.26522.148` requirement from
`Microsoft.AspNetCore.OpenApi` 11, which is what caused the NU1107. Nothing was downgraded, there is no
`VersionOverride`, and the bundle step is still in the pipeline (`aspire-app-host/program.cs:620`).
I found no defects.

## Verification (this round, foreground)

- **Comment claim checked.** The `Microsoft.AspNetCore.OpenApi` 11.0.0-rc.3.26522.148 nuspec depends on
  `Microsoft.Extensions.Hosting` 11.0.0-rc.3.26522.148. The Oakton 6.3.0 nuspec depends on
  `Microsoft.Extensions.Hosting [6.0.0, 10.0.0)`, `JasperFx.Core` 1.9.0, and `Spectre.Console` 0.47.0.
  The csproj comment matches both.
- **Root cause and fix reproduced.** I built a scratch project in /tmp with the same shape as the bundle:
  a `Microsoft.NET.Sdk` exe, net11.0, with a FrameworkReference to `Microsoft.AspNetCore.App` and a
  ProjectReference to web-server. `dotnet build -c Release` restored and built with 0 warnings and
  0 errors. `dotnet publish -c Release -r linux-x64 --no-self-contained`, the bundle's RID mode, also
  succeeded, with no NU1107 and no NU1608.
- **Concern checked: do consumers lose Oakton at runtime?** The concern was that after the change,
  Oakton reaches `web-server-integration-tests` only through api-server: the template-smoke
  `web-server-integration-tests.deps.json` shows `api-server -> Oakton 6.3.0`. In the
  `SmokeNoApi` variant (`--api false`), timewarp-testing's `WebApplicationHost` calls web-server's
  `ConfigureServices`, which calls Oakton's `CheckEnvironment`, so a missing `Oakton.dll` would throw
  FileNotFoundException. The probe has no api-server reference, and it still got `Oakton.dll`,
  `JasperFx.Core.dll`, and `Spectre.Console.dll` in its output. Its deps.json lists them as
  `type: reference` (ResolveAssemblyReference copies dependencies of the referenced web-server
  assembly). Running the probe loaded the `web-server` assembly. No finding.
- web-server's own build and publish output still includes Oakton: `PrivateAssets` only stops the
  package from flowing to consumers, and web-server keeps the compile and runtime assets.
- I did not re-run `./bin/dev workflow --phase publish`, `dev test`, or template-smoke. Those numbers in
  Results are the implementer's. The scratch publish above independently confirms the NU1107 root cause
  and the fix.

## Goal vs task

The `## Goal (implementer restatement)` CI-fix paragraph matches the failure and its constraints:
do not skip or disable the bundle, and do not downgrade a package. That paragraph is also where the task
records this pass. No misread.

## Issues

None.
