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

## Progress

- [x] **Deleted the obsolete `Feature.*` item templates** (2026-06-26). The repo previously shipped
      multiple templates incl. a Console-App template (dropped — Nuru covers that now); the feature
      scaffolders are likewise obsolete. Removed `Feature.Action`, `Feature.AutoCrud`,
      `Feature.Endpoint`, `Feature.State` (real item templates) + `Feature.CrudComponents`,
      `Feature.CrudPages` (loose razor content, no manifest, unreferenced). Cleaned the packaging
      csproj (dropped the now-empty `templates\**\*` Content include + Feature.State None-Remove +
      vestigial `templates/Directory.Build.*`) and stale `.gitignore` lines. `templates/` is now empty.
- [x] **Kebab-renamed the packaging solution** (2026-06-26): `TimeWarp.Templates/` →
      `timewarp-templates/` + all internals (`source/`, `tests/`, `documentation/`, `assets/`,
      `build/`, project dirs/files, `.ps1`/`.cs` files). Updated every internal ref (`.slnx`,
      `ProjectReference`, `docfx.json`, scripts, `index.md`) and external ref (both
      `.github/workflows`, `CLAUDE.md`). Modernized to .NET 10: removed stale `9.0.x` `global.json`
      overrides, bumped CI `setup-dotnet` → `10.0.x`, added a `Directory.Packages.props` that
      disables CPM for the templates tree (test harness pins older versions inline). Solution
      restores clean; root `dev build` green.
- [x] **Content-root repoint + end-to-end verify** (2026-06-26): moved `.template.config/` to the
      repo root; rewrote `template.json` for the kebab layout; repointed the packaging csproj off
      the deleted tree to an allow-list of root content; fixed the `readme.md` path. Fixed two
      template-engine artifacts in source so generated output builds (`user-claims-base.cs`
      `#if false` guarded with `//-:cnd:noEmit`; dropped dead trailing empty `#if/#endif` blocks in
      `aspire-app-host/program.cs`). **VERIFIED**: `dotnet pack` → `dotnet new install` →
      `dotnet new timewarp-architecture -n MyApp` → the full generated multi-project solution builds
      with **0 errors**, namespaces correctly substituted to `MyApp`. `TimeWarp.Architecture/` fully
      removed. Root `dev build` still green.
- [x] **Feature-flag `.slnx` conditionals + blank-line artifacts** (2026-06-26): wrapped the optional
      api/grpc/web/yarp project groups + their integration-test projects in `.slnx` `<!--#if-->`
      markers (verified the engine processes `.slnx`); tightened blank lines around the feature
      `#if` blocks in `web-spa/global-usings.cs` so stripping doesn't leave consecutive blanks
      (IDE2000). **`--grpc false` and `--cosmosdb false` now generate clean-building solutions.**
- [ ] **Remaining — per-feature DECOUPLING (spun out to task 071).** Flag-off combos surfaced genuine
      feature coupling, not template artifacts: `--api`, `--web`, `--yarp`, `--counter`,
      `--eventstream` (124 errors), `--postgres` false each leave code referencing the excluded
      feature without `#if` guards → CS0234/CS0246/RZ10012. A real per-feature decoupling effort,
      distinct from the repoint. Default + grpc + cosmosdb work.

## Status: core repoint COMPLETE & verified

The 064 goal — repoint `dotnet new timewarp-architecture` to the root kebab layout — is DONE and
verified end-to-end (default template builds 0 errors; grpc/cosmosdb flag-off also build). The
remaining per-feature decoupling for the other flags is tracked as task 071 so 064 can close.

## Implementation spec (2026-06-26 — design locked with user)

**Decision: the repo IS the template.** Pack root `source/` + `tests/` in place (no duplicate/staged
tree). Use an **allow-list** of includes (robust — new root dirs don't leak) rather than a whole-repo
deny-list. `.template.config/` moves to the **repo root**.

### Mandatory template content (allow-list — VERIFIED required to build)
- `source/**`, `tests/**`
- `timewarp-architecture.slnx`
- `Directory.Build.props`, `Directory.Packages.props`, `global.json`
- `msbuild/repository.props`  ← `Directory.Build.props` imports it
- `BannedSymbols.txt`         ← referenced as `AdditionalFiles` by `Directory.Build.props`
- `aspire.config.json`        ← points at the app host
- `.editorconfig`, `.gitattributes`, `.gitignore`
- `unlicense.md`, `.template.config/`

### Repo-only — EXCLUDE (never template content)
`kanban/`, `.claude/`, `.agents/`, `.agent/`, `.grok/`, `.github/`, `.githooks/`, `.vscode/`,
`.idea/`, `.playwright/`, `.opencode/`, `.config/`, `skills/`, `evals/`, `spikes/`, `samples/`,
`runfiles/`, `tools/`, `scripts/`, `TimeWarp.Templates/`, `TimeWarp.Architecture/`, `CLAUDE.md`,
`opencode.jsonc`, `.memsearch.toml`, `.mcp.json`, `.vally.yaml`, `.mailmap`, `.envrc`, `readme.md`,
`documentation/` (142 files), `assets/`.
- DECIDED (2026-06-26): exclude BOTH `documentation/` and `assets/` — generated apps start clean
  without the framework's own docs/branding.

### template.json `modifiers` — feature-flag excludes in KEBAB (paths VERIFIED present)
- `(!grpc)`: `source/container-apps/grpc/**`, `source/container-apps/web/web-spa/features/superhero/**`,
  `source/container-apps/web/web-spa/services/superhero-grpc-service-provider.cs`
  (NOTE: no grpc integration tests exist anymore — drop old `Tests/Grpc.Server.Integration.Tests`)
- `(!api)`: `source/container-apps/api/**`,
  `source/container-apps/web/web-spa/features/weather-forecast/**`,
  `source/container-apps/web/web-spa/services/mocks/mock-api-service.cs`,
  `source/container-apps/web/web-spa/services/api-services/api-server-api-service.cs`,
  `tests/common/timewarp-testing/applications/api-test-server-application.cs`,
  `tests/container-apps/api/**`
- `(!web)`: `source/container-apps/web/**`,
  `tests/common/timewarp-testing/applications/{web-test-server-application,spa-test-application}.cs`,
  `tests/container-apps/web/**`
- `(!yarp)`: `source/container-apps/yarp/**`,
  `tests/common/timewarp-testing/applications/yarp-test-server-application.cs`
- `(!cosmosdb)`: `web-infrastructure/persistence/cosmos-db-context.cs`,
  `web-server/hosted-services/cosmos-db-context-startup-hosted-service.cs`,
  `web-server/modules/cosmos-db-module.cs`  (NOTE: no cosmos env-check exists — drop that old exclude)
- `(!postgres)`: `web-infrastructure/persistence/postgres-db-context.cs`,
  `web-infrastructure/configuration/postgres-db-options.cs`,
  `web-server/configuration/environment-checks/postgres-db-environment-check.cs`,
  `web-server/hosted-services/postgres-db-context-startup-hosted-service.cs`,
  `web-server/modules/postgres-db-module.cs`
- `(!counter)`: `source/container-apps/web/web-spa/features/counter/**`
- `(!eventStream)`: `source/container-apps/web/web-spa/features/event-stream/**`  (was `EventStream/`)
- Drop the old `Kanban/**` and `Process/**` excludes (not in the allow-list anyway).

### Packaging csproj (`TimeWarp.Templates/.../TimeWarp.Architecture.csproj`)
- Replace the `..\..\..\TimeWarp.Architecture\**\*` content glob with the allow-list above, packing
  to `content/templates/TimeWarp.Architecture/%(RecursiveDir)...`.
- Fix `PackageReadmeFile`/`<None Include="..\..\..\ReadMe.md">` → root `readme.md` (path is broken).
- Reconcile version: csproj `2.0.0-alpha.11` vs `BuildAndInstallTemplate.ps1` pinned `10.2.3`.

### Verify (the real work — not yet done)
- `dotnet pack` the template csproj → `dotnet new uninstall/install` → `dotnet new timewarp-architecture
  -n MyApp` → generated solution **builds** (FluentUI v5, no Tailwind/npm); toggle feature flags and
  confirm the right folders drop. THIS LOOP IS REQUIRED before 064 can be marked done.

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
