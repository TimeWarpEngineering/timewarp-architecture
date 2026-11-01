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

Move FastEndpoints and FastEndpoints.OpenApi together onto the latest 9.x prerelease
(9.0.0-beta.6 unless nuget.org has a newer 9.x). Pin Microsoft.AspNetCore.OpenApi to the repo's
ASP.NET Core train, `11.0.0-rc.3.26522.148`, so the FastEndpoints net11.0 floor of 11.0.0-rc.2
is lifted. Pin Microsoft.OpenApi to 3.11.0. The comments on that pin, in `Directory.Packages.props`
and `foundation-server.csproj`, have to say what is true after the move: AspNetCore.OpenApi 11
RC3 allows Microsoft.OpenApi `[3.10.2, 4.0.0)`, and the direct reference only lifts 3.10.2 to
3.11.0. Drop the old 2.0.0 / GHSA wording.

Fix the product for FastEndpoints 9 and the OpenApi 3 object model (document transformers,
Scalar, generator output). No `VersionOverride`, and no downgrade to force the graph. If a break
needs a FastEndpoints release we do not have, stop and report the exact error.

Done means the product diff is committed and pushed, the checklist is ticked, and Results record
the gates: `dev build` clean, `dev test` including the integration lane, `dev template-smoke`,
`dev regenerate-generated` with no unintended golden diff, `ganda repo audit`, and
`ganda nuget outdated` with no remaining FastEndpoints or Microsoft.OpenApi update. Results also
record that web-server and api-server still serve the OpenAPI document and the Scalar UI.

## Checklist

- [x] FastEndpoints* on 9.x prerelease
- [x] Microsoft.AspNetCore.OpenApi pinned to the RC train; Microsoft.OpenApi on 3.x; comment rewritten
- [x] FastEndpoints 9 / OpenApi 3 call sites and generator output fixed
- [x] OpenAPI document and Scalar served (evidence recorded)
- [x] Gates green

## Session

- Created: 943834 (2026-09-29), follow-up from task 249
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1960d-9a54-7072-bd5d-d3e0602b0acf, max-turns 500, Done — 2026-11-01T07:56:23Z
- Review oracle round 1: reviewer-claude (claude, model claude-opus-5-5), effort 1, roster general,
  max-turns 80, 2026-11-01. 2 open (M1 bug: uncommitted diff, no Results, gates not recorded;
  M2 suggestion: stale 2.0.0 pin comment). Fix pass requested. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 80 — 2026-11-01T07:59:29Z
- Fix pass 2 (implement-fix-2, 2026-11-01): reworded both pin comments to the 3.10.2 → 3.11.0 lift, ran the gates in the foreground, and committed the product diff. Round 2 is the host's next node.

## Review

- Round 1: Blocked — open M1, M2
- Fix pass 2: M1 and M2 addressed on this branch; round 2 has not run

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

## Results

FastEndpoints and FastEndpoints.OpenApi are `9.0.0-beta.6` (still the latest 9.x on nuget.org).
`Microsoft.AspNetCore.OpenApi` is `11.0.0-rc.3.26522.148`. `Microsoft.OpenApi` is `3.11.0`.
foundation-server `project.assets.json` shows AspNetCore.OpenApi depending on Microsoft.OpenApi
`[3.10.2, 4.0.0)` and resolving `3.11.0`. No `VersionOverride`. The pin comments name that
3.10.2 → 3.11.0 lift. OpenApi 3 call sites use `IOpenApiMediaType`. The request-agent example
assertion reads the `examples` array FastEndpoints 9 writes.

Observed this pass: `./bin/dev build` succeeded with 0 warnings and 0 errors.
`./bin/dev test` passed 23 of 23 projects: api-server-integration-tests 2/2,
web-server-integration-tests 426 passed and 1 skipped, aspire-tests 84/84,
yarp-integration-tests 19/19, and web-spa-playwright-tests 33/33.
`./bin/dev template-smoke` printed `Template smoke SUCCEEDED`; SmokeDefault built with 0 warnings.
`./bin/dev regenerate-generated` exited 0 and left no diff under `goldens/`.
`ganda nuget outdated` checked 133 packages and printed `All packages are up to date!`.

`ganda repo audit` exited 0 (34 passed, 1 advisory, 2 skipped). The advisory is `kanban-landed`:
nine other live claims have no usable worktree (070-001, 104-024, 104-025, 104-026, 123, 141,
198, 244, 454). Those claims are not this task.

### How to validate

Smoke: `./bin/dev build`. `./bin/dev test`. `./bin/dev template-smoke`. `./bin/dev regenerate-generated`, then `git status --short -- goldens`. `ganda repo audit`. `ganda nuget outdated`. Read the comments above the OpenApi pins in `Directory.Packages.props` and `foundation-server.csproj`.

Expect: Build is 0 warnings and 0 errors. `./bin/dev test` reports 23 of 23 projects passed, including api-server-integration-tests 2/2 (OpenAPI document 200 with a WeatherForecasts tag, and `/scalar/v1` 200 containing `openapi/v1.json`), web-server-integration-tests 426 passed and 1 skipped (the suite's OpenAPI and Scalar tests, including `/openapi/v1.json` 200), aspire-tests 84/84, and yarp-integration-tests 19/19. `./bin/dev template-smoke` prints `Template smoke SUCCEEDED` and SmokeDefault builds with 0 warnings. `./bin/dev regenerate-generated` exits 0 and leaves no diff under `goldens/ingress`. `ganda repo audit` exits 0. `ganda nuget outdated` prints `All packages are up to date!`, including FastEndpoints 9.0.0-beta.6 and Microsoft.OpenApi 3.11.0. Neither pin comment mentions Microsoft.OpenApi 2.0.0 or GHSA-v5pm-xwqc-g5wc.
