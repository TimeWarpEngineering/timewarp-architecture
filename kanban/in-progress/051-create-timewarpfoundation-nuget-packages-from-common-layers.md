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
