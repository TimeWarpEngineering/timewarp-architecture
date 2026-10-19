# Round 1 — general
**Date:** 2026-10-19
**Scope reviewed:** branch vs `master`: `timewarp-architecture-template.csproj` `<PackageTags>`, `<Version>` in `source/Directory.Build.props` and `timewarp-templates/Directory.Build.props`, the eleven platform pins in `Directory.Packages.props`, plus the task kitchen.

## Summary

The change removes `Tye` and `pwa` from the shipped template tags and bumps the repo-wide version and the
platform pins from `2.0.0-beta.22` to `2.0.0-beta.23` in one commit. That follows the task-124 pin policy. Risk is low.
The implementer's gate claims hold when re-run in this worktree. The `pwa` removal is justified:
`web-spa.csproj` keeps the `ServiceWorker` items and `ServiceWorkerAssetsManifest` commented out, so no
service worker ships. The implementer restatement matches the task's Description, Checklist, and Notes (the version-bump
condition comes from Notes), so nothing was misread.

## Verification (re-run by reviewer, 2026-10-19)

- `git grep -I -n -i -E '\btye\b|tye\.yaml|Microsoft\.Tye' -- ':!kanban'`: no output (exit 1).
- `git grep -n '2.0.0-beta.22' -- ':!kanban'`: no output, so no pin was left behind.
- `dotnet msbuild … -getProperty:PackageTags,Version`: `dotnet-new;templates;aspnet;blazor;spa;timewarp;gRPC;Aspire;YARP;Clean Architecture;Domain Driven Design` / `2.0.0-beta.23`.
- `./bin/dev check-version`: exit 0. Source is `2.0.0-beta.23`, NuGet is `2.0.0-beta.22`, and it reports "Version in source is new — safe to release."
- `./bin/dev build`: 0 Warning(s), 0 Error(s).
- `ganda repo audit`: "Repository passes all audit checks."
- `kanban/done/` is untouched by the diff.

## Issues

None.

## Observations (not findings; out of this task's scope)

- `timewarp-templates/source/timewarp-architecture-template/Directory.Build.props` still sets a
  space-separated `<PackageTags>`. The csproj overrides it, so the value is dead. It contains no Tye and does
  not ship. The implementer recorded this in Results. Deleting it, along with the legacy `PackageIconUrl` and the
  `PackageReleaseNotes` TODO, would make a reasonable boy-scout follow-up. This task did not touch that file.
- `web-spa/wwwroot/service-worker.published.js` remains in the tree even though the service worker is
  disabled. It is unrelated to Tye and outside this task's scope.
