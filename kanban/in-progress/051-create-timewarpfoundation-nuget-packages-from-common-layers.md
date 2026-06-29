# Create TimeWarp.Foundation NuGet packages from Common.* layers

## Summary

Extract the Common.* projects (Domain, Contracts, Application, Infrastructure, Server) into publishable NuGet packages under the `TimeWarp.Foundation.*` prefix. This enables 5-10 new client projects to reference shared code via NuGets instead of copying source, preventing drift while maintaining DRY principles.

## Checklist

### Planning & Design
- [ ] Confirm package names and namespaces with `TimeWarp.Foundation.*` prefix
- [ ] Define version strategy (all packages version together)
- [ ] Determine CI/CD pipeline location (GitHub Actions workflow)
- [ ] Set up NuGet.org API key for publishing

### Project Renaming & Refactoring
- [ ] Rename `Common.Domain` → `TimeWarp.Foundation.Domain`
- [ ] Rename `Common.Contracts` → `TimeWarp.Foundation.Contracts`
- [ ] Rename `Common.Application` → `TimeWarp.Foundation.Application`
- [ ] Rename `Common.Infrastructure` → `TimeWarp.Foundation.Infrastructure`
- [ ] Rename `Common.Server` → `TimeWarp.Foundation.Server`
- [ ] Update namespaces in all source files from `TimeWarp.Architecture.Common.*` to `TimeWarp.Foundation.*`

### Package Configuration
- [ ] Add NuGet metadata to each .csproj (PackageId, Version, Description, Tags, Author, License)
- [ ] Configure Source Link for better debugging experience
- [ ] Set up deterministic builds
- [ ] Add package README.md files

### Template Updates
- [ ] Update template to reference NuGet packages instead of project references
- [ ] Test template generation works with NuGet packages
- [ ] Document the new package-based approach in template docs

### Publishing
- [ ] Create GitHub Actions workflow for automated NuGet publishing
- [ ] Publish initial 1.0.0 versions of all 5 packages
- [ ] Verify packages appear on nuget.org
- [ ] Test package installation in a sample project

### Per-Repo Agent Integration (for client projects)
- [ ] Design agent instructions for Foundation package version bumps
- [ ] Document migration strategy for breaking changes
- [ ] Create example agent workflow for automated updates

## Notes

### Naming Decisions
- Package prefix: `TimeWarp.Foundation.*`
- Namespace: `TimeWarp.Foundation.*`
- Original project location: `Source/Common/`
- Future location: `Source/Foundation/` or keep as `Source/Common/` (decide)

### Package Names
| Project | NuGet Package |
|---------|---------------|
| Common.Domain | `TimeWarp.Foundation.Domain` |
| Common.Contracts | `TimeWarp.Foundation.Contracts` |
| Common.Application | `TimeWarp.Foundation.Application` |
| Common.Infrastructure | `TimeWarp.Foundation.Infrastructure` |
| Common.Server | `TimeWarp.Foundation.Server` |

### Dependency Graph
```
Foundation.Domain ────────────────┐
                                  │
Foundation.Contracts ─────────────┼──→ Foundation.Application ──→ Foundation.Infrastructure ──→ Foundation.Server
                                  │
TimeWarp.Modules (external) ──────┘
```

### Update Strategy for Client Projects
Each client project (5-10 repos) will have its own AI agent that:
1. Monitors for new Foundation package versions
2. Creates branches for version bumps
3. Runs builds and tests
4. Creates PRs for successful updates
5. Flags breaking changes for manual review

### Why Not Source-Only NuGets?
Regular NuGets are preferred over source-only packages because:
- Simpler consumption (just add PackageReference)
- Better IntelliSense and debugging with Source Link
- Standard .NET ecosystem pattern
- Less complexity in build pipeline

### Breaking Change Considerations
- Breaking changes require major version bumps
- Document migration steps in release notes
- Consider auto-migration scripts for common patterns
- Flag repos needing manual intervention

## References
- Current project locations: `Source/Common/*`
- Template projects will consume these via NuGet instead of project reference
- Client projects are separate repos owned by different clients

## Progress (2026-06-29 — in-repo build-out)

**Phase 1 — namespace rename: DONE & verified.** Renamed all 19 foundation-owned namespaces
`TimeWarp.Architecture.*` → `TimeWarp.Foundation.*` (+ the RouteMixin.mixin HttpVerb type), and
updated every consumer (container-apps, tests, analyzers): 11 exclusive namespaces replaced, 8
shared-with-app namespaces got the Foundation using added per-project, relative/fully-qualified
refs fixed, an `IBaseRequest` alias added in web-spa. Generator: `BaseFastEndpoint` →
`TimeWarp.Foundation.Features`; `RouteMixinAttribute` stays `TimeWarp.Architecture` (Moxy emits it
there). `dev build` green; api integration 6 passed; source-gen 5/5; Enumeration Fixie 21 + Jaribu 21.

**Phase 2 — packaging: DONE & verified.** Added shared packaging props to
`source/foundation/Directory.Build.props` (IsPackable, license, repo URL, tags, SourceLink via
PublishRepositoryUrl/EmbedUntrackedSources, snupkg). Per-project PackageId/Title/Description:
`TimeWarp.Foundation.{Domain,Contracts,Application,Infrastructure,Server}`. Also made the
foundation dependency `timewarp-modules` packable as `TimeWarp.Modules`. All 6 pack cleanly to a
local feed with symbol packages and correct inter-package deps (e.g. Application → Contracts +
Domain + Modules; Server → Infrastructure). Version `1.0.0-beta.1` from TimeWarp.Build.Tasks.

**Phase 3 — CI publish: DONE (in-repo), matches the TimeWarp.Nuru/Amuru pattern.** Publish logic
lives in the mode-aware `dev workflow` command (`tools/dev-cli/endpoints/workflow-command.cs`):
- **Pr/Merge** (push/PR): `clean -> build -> test` (tests gate here).
- **Release** (release/dispatch): `clean -> build -> pack -> push` — **no test step**; a release
  publishes as long as it builds. Packs the 7 publishable projects (6 foundation + template) to
  `artifacts/packages` and pushes `*.nupkg` via Amuru (`DotNet.Pack`, `DotNet.NuGet().Push(...)
  .WithSkipDuplicate()`). Mode auto-detected from `GITHUB_EVENT_NAME`.

`.github/workflows/workflow.yml` is the single canonical CI file (one `ci` job, one "Run CI
Pipeline" step calling `dev workflow`), with a `release: types:[published]` trigger and **OIDC
Trusted Publishing** (`nuget/login@v1`, `id-token: write`, user `TimeWarp.Enterprises`) — no stored
API key. Artifacts uploaded on every run. The standalone `publish-command.cs` and the separate
`publish.yml`/`timewarp-foundation.yml`/`timewarp-architecture.yml` workflows were removed.

*To publish: cut a GitHub Release. Requires nuget.org Trusted Publishing configured for the
`TimeWarp.Foundation.*`, `TimeWarp.Modules`, and `TimeWarp.Architecture` package IDs.*

**First publish DONE (2026-06-29).** PR #261 merged to master; release `v2.0.0-beta.1` cut. OIDC
Trusted Publishing policies configured on nuget.org; the release-mode run (28384817802) went green
end-to-end — OIDC login → clean → build → pack (7) → push all succeeded. All 7 packages published at
`2.0.0-beta.1` (propagating through nuget.org validation/indexing at time of writing). This UNBLOCKS
Phase 4 (template can now PackageReference the published `TimeWarp.Foundation.*` IDs).

**Phase 4 — repoint the template to reference the packages: DONE & verified (amends 064).** The
container-apps reference the foundation layer by project ref in this repo and by `TimeWarp.Foundation.*`
PackageReference in a generated app. The switch is the MSBuild prop `UseFoundationPackages`
(source/Directory.Build.props), auto-detected by whether foundation source is present — so the csproj
switch and the dotnet-new exclusion agree automatically. The template excludes `source/foundation/**`,
`source/libraries/timewarp-modules/**`, `tests/foundation/**` (template.json `foundationPackages`
symbol, default true; matching `.slnx` `<!--#if (!foundationPackages)-->` guards). The contract mixins
travel via the bundled source generator (task **053-001**) — that's what made Phase 4 possible (Moxy
`.mixin` AdditionalFiles don't cross a package boundary). Foundation `PackageVersion`s track `$(Version)`;
`Directory.Version.props` is now in the template content (was missing → generated apps had no version).
Bumped to `2.0.0-beta.2` (package contents changed). **Verified end-to-end:** packed all 7 at beta.2 to
a local feed, `dotnet new timewarp-architecture`, restored + built the generated app's web-contracts and
api-server against the packages — green, with the generator flowing from the package and the FastEndpoint
generator reading the package-generated `RouteMixinAttribute` across the boundary.

*Note: actual nuget.org republish at beta.2 happens when the next release is cut (beta.1 is live).*

## Remaining (external)

- **Phase 5 — per-repo client update agents.** Lives in the separate client repos, not here.
