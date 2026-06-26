# Repoint the template to the root layout

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown). **The big one.**

## Why

`.template.config/template.json` is the template definition and still describes the
pre-migration layout — every `sources`/`modifiers` exclude path is `Source/ContainerApps/...`
and `Tests/...` (old PascalCase tree that no longer exists). Until it's repointed at the root
kebab layout (`source/container-apps/...`, `tests/...`), `dotnet new timewarp-architecture` is
broken. This is what unblocks 047's "verify `dotnet new` builds clean" criterion.

## Findings (2026-06-26 recon — legacy tree now gutted)

The legacy `TimeWarp.Architecture/` tree has been emptied down to just `.template.config/`
(`template.json` + `icon.png`) by the teardown work; everything else (DevOps, Scripts, .ai,
devcontainer, build props, docs, one-offs) was deleted or relocated to root. This makes the
template breakage concrete and worse than first described:

- **The main template content root is the dead tree.** The packaging project
  `TimeWarp.Templates/Source/TimeWarp.Architecture.Template/TimeWarp.Architecture.csproj` packs the
  main project template via:
  `<Content Include="..\..\..\TimeWarp.Architecture\**\*" PackagePath="content\templates\TimeWarp.Architecture\..." />`
  That glob now resolves to essentially nothing (only `.template.config/`). The real project
  content lives at root `source/` + `tests/`. So `dotnet new timewarp-architecture` currently
  produces an empty/broken solution — this is now blocking, not theoretical.
- **Two templates, one package.** The `TimeWarp.Architecture` NuGet packs BOTH:
  (a) the main project template (definition = `TimeWarp.Architecture/.template.config/template.json`,
  content = the `..\..\..\TimeWarp.Architecture\**\*` glob above), and
  (b) the item/feature templates under `TimeWarp.Templates/.../templates/` (Feature.Action,
  Feature.AutoCrud, Feature.Endpoint, Feature.State — each with its own `.template.config`).
  064 is about (a); the Feature.* item templates (b) are separate and already function.
- Packaging also references `..\..\..\ReadMe.md` (now root `readme.md` — PATH BROKEN by the
  README kebab-rename) and `..\..\Assets\Logo.png`. Fix these path refs when repointing.
- `BuildAndInstallTemplate.ps1` pins two different versions inline (`10.2.3` vs the csproj's
  `2.0.0-alpha.11`) — reconcile while touching it.

**Net:** the fix is to move the main-template content root from the soon-to-be-deleted
`TimeWarp.Architecture/` glob to the root `source/` + `tests/` layout, rewrite `template.json`
excludes to kebab paths, and fix the `readme.md`/assets path refs. Only after this can
`TimeWarp.Architecture/.template.config/` move/delete and the legacy dir be removed (closes 067).

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
