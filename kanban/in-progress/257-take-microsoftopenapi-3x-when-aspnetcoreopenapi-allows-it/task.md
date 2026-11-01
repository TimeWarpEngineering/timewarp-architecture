# Take Microsoft.OpenApi 3.x when AspNetCore.OpenApi allows it

## Description

`Directory.Packages.props` pins `Microsoft.OpenApi` 2.12.2 (NU1903 lift over the 2.0.0 that
FastEndpoints.OpenApi / Microsoft.AspNetCore.OpenApi pull). `ganda nuget outdated` reports
3.10.2, but every Microsoft.AspNetCore.OpenApi 10.0.x nuspec (re-checked at 10.0.12, task 249)
declares `Microsoft.OpenApi [2.12.0, 3.0.0)`, so a 3.x pin raises NU1608 (warning-as-error).
Deferred from tasks 201 and 249.

**Decision 2026-11-01 (Steven):** do not wait for a stable FastEndpoints. The repo already runs
on the .NET 11 RC train, and the 10.0.11 `Microsoft.AspNetCore.OpenApi` comes in only through
FastEndpoints.OpenApi 8.3.0. Take the FastEndpoints 9 prerelease, whose net11.0 group depends on
`Microsoft.AspNetCore.OpenApi` 11.x, which allows `Microsoft.OpenApi` [3.10.2, 4.0.0).

## Requirements

- Move every `FastEndpoints*` package to the latest 9.x prerelease on nuget.org (9.0.0-beta.6 or
  newer at implement time). Keep them all on one version.
- Pin `Microsoft.AspNetCore.OpenApi` to the repo's ASP.NET Core train (11.0.0-rc.3.26522.148, or
  whatever the other `Microsoft.AspNetCore.*` pins are at implement time). It lifts the
  FastEndpoints 9 net11.0 dependency (11.0.0-rc.2) to the current RC.
- Pin `Microsoft.OpenApi` to the latest 3.x (3.11.0 or newer). Rewrite the long pin comment in
  `Directory.Packages.props` down to what is true after this change. The task-by-task re-check
  history is no longer needed.
- Fix product code for the FastEndpoints 9 and Microsoft.OpenApi 3 breaking changes: document
  transformers, Scalar wiring, the FastEndpoint source generator output
  (`source/analyzers/timewarp-architecture-analyzers/`), and anything that uses the OpenApi object
  model (`Microsoft.OpenApi.Models` types moved in 3.x). Read the FastEndpoints 9 release notes.
- No `VersionOverride`. Do not downgrade anything to satisfy the graph. If FastEndpoints 9 needs a
  feature or fix that only its maintainers can ship, stop and report with the exact error.
- Gates: `dev build` 0 warnings and 0 errors; `dev test` including the integration lane
  (`aspire-tests`, `web-server-integration-tests`, `api` and `yarp` tests); `dev template-smoke`;
  `dev regenerate-generated` with no diff under `goldens/ingress` (or regenerate and commit if a
  route changed on purpose); `ganda repo audit`; `ganda nuget outdated` reports no remaining
  Microsoft.OpenApi or FastEndpoints update. Record that the OpenAPI document and the Scalar UI
  are still served by web-server and api-server (an integration test asserting the document,
  or a recorded `curl` of `/openapi/*.json` returning 200 with the expected paths).

## Goal (implementer restatement)

The repo still pins FastEndpoints 8.3.0, which pulls Microsoft.AspNetCore.OpenApi 10.0.11 and
locks Microsoft.OpenApi below 3.0. Take FastEndpoints 9.0.0-beta.6 (latest 9.x prerelease on
nuget.org at implement time; FastEndpoints and FastEndpoints.OpenApi stay on that same version),
pin Microsoft.AspNetCore.OpenApi to the existing ASP.NET Core RC train
`11.0.0-rc.3.26522.148` so the FastEndpoints net11.0 floor of 11.0.0-rc.2 is lifted, and pin
Microsoft.OpenApi to 3.11.0, the latest 3.x. Replace the long historical pin comment with the
constraint that is true after the move.

Then make the product compile and behave on those packages: OpenAPI document transformers,
Scalar wiring, and the FastEndpoints source generator. Do not use VersionOverride and do not
downgrade another package to force the graph. If a break can only be fixed by a FastEndpoints
release we do not have, stop and report the exact error.

Done means `dev build` is clean, `dev test` covers the integration lane, `dev template-smoke`
and `dev regenerate-generated` pass (commit a golden only when a route change is intentional),
`ganda repo audit` passes, and `ganda nuget outdated` shows no remaining FastEndpoints or
Microsoft.OpenApi update. The OpenAPI document and Scalar UI still respond on web-server and
api-server, with that evidence written under Results.

## Checklist

- [ ] FastEndpoints* on 9.x prerelease
- [ ] Microsoft.AspNetCore.OpenApi pinned to the RC train; Microsoft.OpenApi on 3.x; comment rewritten
- [ ] FastEndpoints 9 / OpenApi 3 call sites and generator output fixed
- [ ] OpenAPI document and Scalar served (evidence recorded)
- [ ] Gates green

## Session

- Created: 943834 (2026-09-29), follow-up from task 249
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1960d-9a54-7072-bd5d-d3e0602b0acf, max-turns 500, Done — 2026-11-01T07:56:23Z

## Notes

- Task 249 Results record the 10.0.12 nuspec check.
- Re-checked task 392 (2026-10-20): FastEndpoints.OpenApi 8.3.0 (latest stable) still pins
  Microsoft.AspNetCore.OpenApi 10.0.11, range [2.7.5, 3.0.0); pin moved to 2.13.0.
  FastEndpoints.OpenApi 9.0.0-beta.2 has a net11.0 group on Microsoft.AspNetCore.OpenApi
  11.0.0-rc.2.26504.105, range [3.10.0, 4.0.0). The gate opens once this repo takes
  FastEndpoints 9 (currently beta).
- Re-checked task 456 (2026-10-30): Microsoft.OpenApi 3.11.0 restore of foundation-server raised
  NU1608. Microsoft.AspNetCore.OpenApi 10.0.11 (the package FastEndpoints.OpenApi 8.3.0, latest
  stable, still depends on) requires Microsoft.OpenApi (>= 2.7.5 && < 3.0.0). FastEndpoints.OpenApi
  8.4.0-beta.26 is net10.0 only and depends on Microsoft.AspNetCore.OpenApi 10.0.13
  ([2.12.2, 3.0.0)). 9.0.0-beta.6's net11.0 group still depends on Microsoft.AspNetCore.OpenApi
  11.0.0-rc.2.26504.105, not 11.0.0-rc.3.26522.148 (range [3.10.2, 4.0.0)). Latest 2.x remains
  2.13.0. This task did not take FastEndpoints 9 betas.
