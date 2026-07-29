# File naming

This template uses **kebab-case** for files and folders. Types and namespaces stay **PascalCase**.

## Defaults

| Kind | Example |
|------|---------|
| Source | `global-usings.cs`, `create-role-handler-application.cs` |
| Folders | `features/admin/roles/`, `web-server-integration-tests/` |
| Tests | `create-role-endpoint-tests.cs` (not `CreateRole_Endpoint_Tests.cs`) |
| Docs | `how-to-release.md` preferred over `HowToRelease.md` |

Multi-dot **partial** stems are valid when each segment is kebab:
`application-state.close-modal.cs`.

## Explicit exceptions

| Kind | Rule |
|------|------|
| Blazor | `.razor` basenames match the component type (PascalCase); keep `HomePage.razor` + `HomePage.razor.cs` + `HomePage.razor.css` paired. Do not rename to `home-page.razor`. |
| MSBuild | `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `BannedSymbols.txt` |
| ASP.NET host | `Properties/`, `launchSettings.json`, `appsettings.json`, `appsettings.<Environment>.json` |
| Blazor host glue | `_Imports.razor` / `_imports.razor`, `App.razor` when the host template requires those names |
| Roslyn | `AnalyzerReleases.Shipped.md`, `AnalyzerReleases.Unshipped.md` |

## Product extras

- Feature cohesive grammar (`name[-function]-layer.cs`, TWA0015/0016): skill **`tw-feature-placement`**.
- Slice isolation: skill **`tw-slice-isolation`**.

## Enforcement

| Layer | Scope |
|-------|--------|
| Agent skill **`tw-csharp`** | Conventions + exceptions (all TimeWarp repos) |
| **`TW0001`** (`TimeWarp.SourceGenerators`) | `.cs` basenames in compilations only (`TW*`, not Architecture `TWA*`) |
| Ganda **`kebab-path-names`** (task 188) | Folders + non-`.cs` basenames via `ganda repo audit` (when shipped) |
| Architecture **TWA0015/0016** | Axis-1 feature/platform grammar only |

Decision history (flow repo): ADR-0013 adopt kebab-case file naming.
