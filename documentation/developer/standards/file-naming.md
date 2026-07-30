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
- Co-located Jaribu runfiles under `source/<family>/features/`/`platform/` use the SAME grammar,
  ending `-tests.cs` (a registered-**unrouted** layer — matched and validated by TWA0015/0016
  and the membership guard exactly like a routed layer, but claiming no layer project's build;
  enforcement only fires when the file itself is compiled, e.g. standalone `dotnet run` — NOT the
  `dev build` solution gate, which never touches it). Runfile preamble convention (`PublishAot=false`,
  the `cnd:noEmit`-escaped `JARIBU_MULTI` switch): skill **`tw-feature-placement`** (Co-located
  Jaribu runfile preamble section); reference implementations `create-role-tests.cs`,
  `get-weather-forecasts-tests.cs`. Jaribu itself (attributes, naming, assertions, testing
  philosophy): cross-repo skill **`tw-jaribu`**.

## Enforcement

| Layer | Scope |
|-------|--------|
| Agent skill **`tw-csharp`** | Conventions + exceptions (all TimeWarp repos) |
| **`TW0001`** (`TimeWarp.SourceGenerators` ≥ beta.10) | `.cs` basenames (repo-wide PackageReference + `.editorconfig` warning); skips `obj/`/`bin/` |
| Ganda **`kebab-path-names`** (task 188, shipped) | Folders + non-`.cs` basenames via `ganda repo audit` |
| Architecture **TWA0015/0016** | Axis-1 feature/platform grammar only |

Decision history (flow repo): ADR-0013 adopt kebab-case file naming.
