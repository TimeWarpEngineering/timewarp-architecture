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

Round 2 left two comments to restore. The RC-train facts the pin-comment rewrite deleted go back as short comments beside the pins they explain, with no task-by-task history. Shared-framework Microsoft.* and System.* pins on this train are `11.0.0-rc.3.26522.148` and match SDK `11.0.100-rc.3.26522.148`. `Npgsql.EntityFrameworkCore.PostgreSQL` `11.0.0-rc.3` pins `Microsoft.EntityFrameworkCore` exactly, so the EF family and Npgsql move together; moving EF alone fails restore with NU1608/NU1107. Extensions monthly packages are `10.11.0` and `System.ServiceModel.Primitives` is `10.1.0` because neither line has an 11.x. The auth-schemes test Design region drops the FastEndpoints 8.3 and ASP.NET Core 10 version numbers. This pass is done when those comments are in place, the diff is committed and pushed, and Results record the gates run for the comment-only change.

PR #621's publish job is red. `aspire publish` fails at `web-migrations-generate-migration-bundle` after `Build failed. Use --verbose to see errors.` The migration script step before it succeeds, and master publish is green, so this branch caused it. Reproduce with a verbose `dotnet ef migrations bundle` (framework-dependent, current RID — `PublishAsMigrationBundle()` does not pass `--self-contained`) and then `./bin/dev workflow --phase publish`. Fix the cause. Do not skip or disable the bundle step, and do not downgrade a package to force the graph. This pass is done when publish succeeds locally, the root cause is in Results, and the fix is committed and pushed.

## Checklist

- [x] FastEndpoints* on 9.x prerelease
- [x] Microsoft.AspNetCore.OpenApi pinned to the RC train; Microsoft.OpenApi on 3.x; comment rewritten
- [x] FastEndpoints 9 / OpenApi 3 call sites and generator output fixed
- [x] OpenAPI document and Scalar served (evidence recorded)
- [x] Gates green
- [x] RC-train facts restored beside the pins they explain
- [x] Auth-schemes Design region no longer names FastEndpoints 8.3 or ASP.NET Core 10
- [x] Publish migration bundle restores (Oakton stays on web-server; NU1107 gone)

## Session

- Created: 943834 (2026-09-29), follow-up from task 249
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1960d-9a54-7072-bd5d-d3e0602b0acf, max-turns 500, Done — 2026-11-01T07:56:23Z
- Review oracle round 1: reviewer-claude (claude, model claude-opus-5-5), effort 1, roster general,
  max-turns 80, 2026-11-01. 2 open (M1 bug: uncommitted diff, no Results, gates not recorded;
  M2 suggestion: stale 2.0.0 pin comment). Fix pass requested. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 80 — 2026-11-01T07:59:29Z
- Fix pass 2 (implement-fix-2, 2026-11-01): reworded both pin comments to the 3.10.2 → 3.11.0 lift, ran the gates in the foreground, and committed the product diff. Round 2 is the host's next node.
- Review oracle round 2: reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), effort 2,
  roster general, max-turns 120, session not reported, 2026-11-01. M1 and M2 fixed (the build was
  re-run here: 0 warnings, 0 errors). 2 new open findings: M3 (suggestion: the pin comment rewrite
  deleted still-true RC train / EF-Npgsql / 10.x Extensions facts) and M4 (nit: stale FastEndpoints
  8.3 in the auth-schemes test Design region). Fix pass requested. See `review/round-2/merged.md`.
- Fix pass 3 (implement-fix-3, 2026-11-01): restored the RC-train facts as short comments beside the pins they explain, and dropped the FastEndpoints 8.3 and ASP.NET Core 10 version numbers from the auth-schemes Design region. `./bin/dev build` was 0 warnings and 0 errors. `ganda repo audit` exited 0 with the same `kanban-landed` advisory for nine other claims.
- Review oracle round 3: reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), effort 1,
  roster general, max-turns 80, session not reported, 2026-11-01. Reviewed the delta since round 2
  (925b61eb3, comments only). M3 and M4 are fixed, and there are no new findings. Disposition: clean.
- CI fix (implement, 2026-11-01): PR #621 `ci (publish)` run 41289504501 failed in
  `web-migrations-generate-migration-bundle` with `Build failed. Use --verbose to see errors.`
  Verbose `dotnet ef migrations bundle` showed NU1107. Oakton on web-server is now
  `PrivateAssets=all`. `./bin/dev workflow --phase publish` exited 0.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1964e-f873-7de2-9d80-ba952797338c, max-turns 500, Done — 2026-11-01T09:28:27Z
- Review oracle round 4: reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), effort 2,
  roster general, max-turns 120, session not reported, 2026-11-01. Reviewed the delta since the round 3
  land (15fef86b0, Oakton `PrivateAssets=all`). Reproduced the bundle-shaped restore and the linux-x64
  `--no-self-contained` publish in a scratch project; both were clean, with no NU1107. Checked that
  consumers that do not reference api-server (the `--api false` variant) still get `Oakton.dll` as a
  RAR `reference` dependency, so the in-process web-server tests can still run. No new findings.
  Disposition: clean.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 120 — 2026-11-01T09:34:14Z

## Review

- Round 1: Blocked — open M1, M2
- Fix pass 2: M1 and M2 addressed on this branch
- Round 2: Blocked — open M3, M4. Counts: bug 0 open / 1 fixed, suggestion 1 open / 1 fixed, nit 1 open. Paths: `review/review-framework.md`, `review/round-2/merged.md`
- Fix pass 3: M3 and M4 addressed on this branch
- Round 3: Done
- CI fix: publish migration bundle NU1107 (15fef86b0)
- Round 4: Done
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

Observed on the product pass: `./bin/dev build` succeeded with 0 warnings and 0 errors.
`./bin/dev test` passed 23 of 23 projects: api-server-integration-tests 2/2,
web-server-integration-tests 426 passed and 1 skipped, aspire-tests 84/84,
yarp-integration-tests 19/19, and web-spa-playwright-tests 33/33.
`./bin/dev template-smoke` printed `Template smoke SUCCEEDED`; SmokeDefault built with 0 warnings.
`./bin/dev regenerate-generated` exited 0 and left no diff under `goldens/`.
`ganda nuget outdated` checked 133 packages and printed `All packages are up to date!`.

`ganda repo audit` exited 0 (34 passed, 1 advisory, 2 skipped). The advisory is `kanban-landed`:
nine other live claims have no usable worktree (070-001, 104-024, 104-025, 104-026, 123, 141,
198, 244, 454). Those claims are not this task.

Fix pass 3 restored the RC-train facts beside the pins they explain. Shared-framework Microsoft.* and System.* pins on this train match SDK `11.0.100-rc.3.26522.148`. The EF family and `Npgsql.EntityFrameworkCore.PostgreSQL` move together. Extensions monthly packages (`Telemetry.Abstractions`, `ServiceDiscovery.Yarp`, `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Compliance.Redaction`, `Http.Resilience`, `ServiceDiscovery`, `Telemetry`, `Diagnostics.Testing`) are `10.11.0` because they have no 11.x. `System.ServiceModel.Primitives` is `10.1.0` for the same reason. The auth-schemes Design region names FastEndpoints `Policies()` and `AuthSchemes()` against ASP.NET Core `PolicyEvaluator.AuthenticateAsync`, with no 8.3 or ASP.NET Core 10 version. This pass re-ran `./bin/dev build` (0 warnings, 0 errors, including `web-server-integration-tests`) and `ganda repo audit` (exit 0, same advisory). The integration suite, template smoke, regenerate-generated, and nuget outdated were not re-run: the diff is comments only, and the earlier pass already recorded those gates.

PR #621 publish failed while generating the EF migration bundle. `PublishAsMigrationBundle()` does not pass `--self-contained`. `dotnet ef` still publishes a generated `Microsoft.NET.Sdk` exe with `--runtime linux-x64 --no-self-contained`. That project re-resolves package nuspecs. `Microsoft.AspNetCore.OpenApi` 11.0.0-rc.3.26522.148 requires `Microsoft.Extensions.Hosting` >= 11.0.0-rc.3.26522.148. Oakton 6.3.0 (latest on nuget.org; no net10/net11 dependency group) requires Hosting `[6.0.0, 10.0.0)`. The Web SDK prunes the OpenApi Hosting dependency against the shared framework, so `dev build` and the migration script step stay green. The generated exe does not, and restore fails with NU1107. No newer Oakton exists, and the graph was not downgraded. Oakton on web-server is `PrivateAssets=all`: the host still compiles and publishes against it, and the package constraint does not flow to the bundle project. A local bundle (115 MB) loaded `PostgresDbContext` and stopped on a refused connection to 127.0.0.1:5432, which is the offline design-time path with no Postgres running.

`./bin/dev workflow --phase publish` exited 0. The Release build inside it was 0 warnings and 0 errors. Compose, kubernetes, aca, and aca-neon each finished; compose, aca, and aca-neon generated the migration bundle successfully (kubernetes publishes the SQL script only). Production-safety tests were 47/47 and `helm lint` reported 0 chart failures. `./bin/dev test --no-build` passed 23 of 23 projects, including api-server-integration-tests, web-server-integration-tests, aspire-tests, yarp-integration-tests, web-spa-integration-tests (578), and web-spa-playwright-tests (33). `./bin/dev template-smoke` printed `Template smoke SUCCEEDED`; SmokeDefault built with 0 warnings. `./bin/dev regenerate-generated` exited 0 and left no diff under `goldens/`. `ganda nuget outdated` checked 133 packages and printed `All packages are up to date!`. `ganda repo audit` exited 0 with the same `kanban-landed` advisory for the nine other claims.

### Review disposition

Disposition is `clean`, after 4 rounds. Each round was effort 1 or 2 with a single general
reviewer, reviewer-claude (claude-opus-5-5). Round 4 reviewed the publish-bundle fix (15fef86b0).
It reproduced the NU1107 fix in a bundle-shaped scratch project and raised no findings.

Final counts:
- bug: 0 open, 1 fixed (M1)
- suggestion: 0 open, 2 fixed (M2, M3)
- nit: 0 open, 1 fixed (M4)

There are no wontfix findings and no escalations. Paths:
- `review/review-framework.md`
- `review/round-4/merged.md`
- `review/disposition.md`

### How to validate

Smoke: `./bin/dev workflow --phase publish`. Read the Oakton `PackageReference` in `source/container-apps/web/projects/web-server/web-server.csproj`.

Expect: The workflow exits 0. Compose, aca, and aca-neon each log `migration bundle generated successfully`. The Oakton reference has `PrivateAssets` set to `all`, and the comment names the Hosting `[6.0.0, 10.0.0)` ceiling against OpenApi 11's Hosting 11 requirement. The bundle step is still in the publish pipeline.
