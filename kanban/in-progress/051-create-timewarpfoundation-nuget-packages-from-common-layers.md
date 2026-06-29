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

**Phase 3 — CI publish: DONE (in-repo), logic moved to C#.** Publish logic now lives in the `dev`
CLI: `tools/dev-cli/endpoints/publish-command.cs` (`dev publish --target foundation|template|all`)
packs + pushes via Amuru (`DotNet.Pack` / `DotNet.NuGet().Push(...).WithSkipDuplicate()`), reading
the key from `NUGET_API_KEY`. The two YAML-heavy workflows (`timewarp-foundation.yml`,
`timewarp-architecture.yml`) were replaced by one thin trigger `.github/workflows/publish.yml` that
just calls `dotnet run tools/dev-cli/dev.cs -- publish`. Single repo version → all packages move
together, so `--target all` + `--skip-duplicate` is idempotent (only the bumped version pushes).
*Requires the existing `PUBLISH_TO_NUGET_ORG` secret (mapped to `NUGET_API_KEY`) to publish.*

## Remaining (blocked / external)

- **Phase 4 — repoint the template to reference the packages (amends 064).** BLOCKED on the
  packages being published: a generated app can't `PackageReference` `TimeWarp.Foundation.*` until
  they exist on the feed. Once published, exclude `source/foundation/**` from the template content
  and swap the container-apps' foundation `<ProjectReference>` for `<PackageReference>` (the repo
  keeps project refs for local dev; the template emits package refs). Verify via `dotnet new` + build.
- **Phase 5 — per-repo client update agents.** Lives in the separate client repos, not here.
