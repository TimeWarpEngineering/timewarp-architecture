# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** branch vs master: AppHost program.cs yarp run-mode endpoints, tests/common/closed-box-app-host, the three run-mode boots, the guard tests, csproj links, template.json flag exclusions.

## Summary

The change clears the `dev run` ingress pins for closed-box AppHosts through one linked helper, and it fixes a second port source (the yarp launch profile applicationUrl) by making configuration the only run-mode port source. `dev run` behaviour is unchanged when the pins parse (https pinned, http pinned in-process with IsProxied=false). The Design regions are updated in both the AppHost and the helper. Re-verified: `ClosedBoxAppHost_Should_` passed 4/4 locally (the aspire-tests build had 0 errors). Overall risk is low. No bugs found.

## Issues

### Issue 1 — Severity: nit
- File: tests/container-apps/aspire/aspire-tests/closed-box-app-host-guard-tests.cs:48
- Description: The source scan flags a file only when `CreateAsync<Projects.aspire_app_host>` and `.StartAsync(` appear in the **same** file. A boot that creates the builder in one file and starts it in another would get past the scan.
- Suggestion: Accept this. Every current boot is a single file, and the model fact `Ingress_Endpoints_Should_Not_Use_DevRun_Ports` independently checks the helper's output.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/aspire/aspire-tests/closed-box-app-host-guard-tests.cs:86
- Description: `Default_Config_Should_Keep_DevRun_Pins` reads ambient configuration (appsettings.Development.json plus user secrets and env vars). A maintainer who overrides `Ingress:Port` locally would see a false failure.
- Suggestion: Accept this. The test intentionally asserts the committed `dev run` defaults, and the AppHost Design region says only PublicUrl belongs in user secrets.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/container-apps/aspire/aspire-tests/closed-box-app-host-guard-tests.cs:72
- Description: The model facts resolve the `ingress` resource, but aspire-tests is not excluded under `(!yarp)` in template.json, so a `--yarp false` generated app would fail these facts.
- Suggestion: This is pre-existing. `ingress-smoke-tests.cs` has the same unguarded `ingress` dependency and is not excluded either. Leave it to whoever owns non-default flag-matrix test coverage. It is not a regression from this change.
- Status: open
