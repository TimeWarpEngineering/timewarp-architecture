# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/524-remove-api-debug-config-endpoint-from-common` vs merge-base `f5c492088`, against the original task and `## Goal (implementer restatement)`. Gates were not re-run. The implementer's `## Results` and `### How to validate` cover `./bin/dev build` (0 warnings, 0 errors), the filtered `CommonServerModuleEndpoints_` test (1/1), and `ganda repo audit` for tree `2cb2418434cd`.

## Summary

`CommonServerModule.ConfigureEndpoints` is an empty `IAspNetModule` hook. The `MapGet("/api/debug-config")` registration and `GetDebugView()` call are gone. `api-server` and `web-server` still call the same method. The Design region, class summary, and method summary in `common-server-module.cs`, and the Design region in `open-api-discovery-coverage.cs`, no longer name the route. `Microsoft.Extensions.Hosting` left `foundation-server` `global-usings.cs`; that project no longer mentions `IsDevelopment` or other Hosting types. A search of `source/`, `tests/`, and `skills/` finds `debug-config` only as the new test's request path, and finds no `GetDebugView`.

The new Jaribu test builds a tiny `UseTestServer()` host with `EnvironmentName = Environments.Development`, asserts `IsDevelopment()`, calls `CommonServerModule.ConfigureEndpoints`, and expects `GET /api/debug-config` to be 404. It matches the host shape in `open-api-discovery-coverage-tests.cs` and does not start Docker. The recorded build, filtered test, and audit cover this diff.

Two wording gaps in the restatement are recorded under `## Learnings` and are not findings. The test's request string is required by the original task. The test lives beside the cited pattern file, so `ci (integration)` runs it on pull requests and nightly `template-smoke --app-tests` runs it in a generated app. `tests/foundation/**` is excluded from the template, so that tree cannot serve both `ci (fast)` and `template-smoke`.

## Issues

<!-- None. -->
