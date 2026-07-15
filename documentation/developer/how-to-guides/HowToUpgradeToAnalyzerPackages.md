# How to upgrade to analyzer NuGet packages

Greenfield solutions from `dotnet new timewarp-architecture` (with `analyzerPackages` default
**true**) already reference:

| Package | Role |
|---------|------|
| `TimeWarp.Architecture.Analyzers` | Convention rules TWPA0002–0010 (wired via `Directory.Build.props`) |
| `TimeWarp.Architecture.Generators` | Source generators + TWPA0001 (`web-spa`, `api-server`, …) |
| `TimeWarp.Architecture.Attributes` | Runtime attributes such as `[ApiEndpoint]` |

Older generated apps may still own a **copy** of `source/analyzers/**` and `tests/analyzers/**`
via `ProjectReference` / `OutputItemType=Analyzer`. This guide moves those apps to packages so
rule and generator fixes roll forward with a version bump instead of a source merge.

## 1. Pin package versions (CPM)

In the app’s root `Directory.Packages.props`, under the TimeWarp package versions, add (or bump):

```xml
<PackageVersion Include="TimeWarp.Architecture.Analyzers" Version="2.0.0-beta.3" />
<PackageVersion Include="TimeWarp.Architecture.Generators" Version="2.0.0-beta.3" />
<PackageVersion Include="TimeWarp.Architecture.Attributes" Version="2.0.0-beta.3" />
```

Use the latest published versions from NuGet — they may lag the architecture repo’s in-dev
`<Version>`.

## 2. Wire dual-mode (or package-only) in Directory.Build.props

**Option A — match the template** (works if you delete analyzer source later):

In `source/Directory.Build.props` and `tests/Directory.Build.props`, replace the hard-coded
`ProjectReference` to `timewarp-architecture-convention-analyzers` with the dual-mode switch used
in this repository: `UseAnalyzerPackages` is **true** when
`source/analyzers/timewarp-architecture-convention-analyzers/….csproj` is missing; otherwise
**false**. Project mode keeps the `ProjectReference`; package mode uses:

```xml
<PackageReference Include="TimeWarp.Architecture.Analyzers" PrivateAssets="all" />
```

Keep `<CompilerVisibleProperty Include="TimeWarpSliceRoot" />` in both modes (TWPA0009).

**Option B — package-only** (if you are deleting analyzer source in the same change): drop the
project refs and always use the three package references as in step 3.

## 3. Switch consumer projects

| Project | Project ref (old) | Package (new) |
|---------|-------------------|---------------|
| Almost everything (via props) | convention-analyzers `OutputItemType=Analyzer` | `TimeWarp.Architecture.Analyzers` `PrivateAssets=all` |
| `web-spa`, `api-server` | analyzers (generators) `OutputItemType=Analyzer` | `TimeWarp.Architecture.Generators` `PrivateAssets=all` |
| `api-server`, `api-contracts` | attributes project | `TimeWarp.Architecture.Attributes` |

Do **not** leave both a ProjectReference and a PackageReference for the same consumer.

## 4. Remove vendored analyzer source

After restore succeeds against packages:

1. Delete `source/analyzers/` and `tests/analyzers/` (if present).
2. Remove those projects from the solution file.
3. Confirm `UseAnalyzerPackages` evaluates to true (or you used package-only wiring).

## 5. Verify

```console
dotnet restore
dotnet build   # 0 warnings / 0 errors — warnings are errors in this stack
```

Spot-check:

- A missing `#region Purpose` still reports **TWPA0004** (convention package).
- `api-server` still emits FastEndpoint generated files when
  `EnableApiEndpointGeneration` is true (generators package).
- `[ApiEndpoint]` contracts still compile (`Attributes` package).

## Notes

- Diagnostic IDs stay **TWPA####**; this upgrade is distribution only.
- Generators must **not** be referenced repo-wide — only on projects that should run them
  (`web-spa`, `api-server`). Convention analyzers remain the repo-wide package.
- Trusted Publishing / NuGet availability: packages ship on the architecture repo’s release
  workflow alongside foundation packages.
