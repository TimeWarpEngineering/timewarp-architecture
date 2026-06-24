# Repoint the template to the root layout

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown). **The big one.**

## Why

`.template.config/template.json` is the template definition and still describes the
pre-migration layout — every `sources`/`modifiers` exclude path is `Source/ContainerApps/...`
and `Tests/...` (old PascalCase tree that no longer exists). Until it's repointed at the root
kebab layout (`source/container-apps/...`, `tests/...`), `dotnet new timewarp-architecture` is
broken. This is what unblocks 047's "verify `dotnet new` builds clean" criterion.

## Scope

- [ ] Decide the template ROOT: the template now needs to package the repo-root `source/` +
      `tests/` (+ devops/docs?) rather than the `TimeWarp.Architecture/` subdir. Determine the
      `sourceName`, the template content root, and how TimeWarp.Templates packaging references it.
- [ ] Rewrite all `template.json` `modifiers` exclude globs to the kebab layout
      (`source/container-apps/{web,api,grpc,yarp,aspire}/...`, `tests/container-apps/...`,
      `tests/common/timewarp-testing/...`). Update the feature-flag conditions (grpc/api/web/yarp/
      cosmosdb/counter/eventstream/postgres) to the new paths.
- [ ] Reconcile the symbol set vs current features (e.g. the dropped Grpc.Server.Integration.Tests,
      EndToEnd tests, TimeWarp.Automation — remove stale excludes).
- [ ] TimeWarp.Templates packaging: `BuildAndInstallTemplate.ps1` + the `.csproj`/nuspec that packs
      the template — repoint at the new content root.
- [ ] Verify: `dotnet new timewarp-architecture -n MyApp` produces a solution that builds + runs
      (FluentUI v5, no Tailwind/npm), with feature flags toggling the right folders.

## Notes

- The npm `postAction` was already removed from `template.json`.
- This likely interacts with the docs move (062) and devops move (063) if those become template
  content too — sequence accordingly.
- Biggest risk/effort in the whole teardown; treat as its own focused effort.
