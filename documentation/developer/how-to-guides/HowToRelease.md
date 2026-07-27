# How to release TimeWarp.Architecture

This guide covers cutting a platform + template release and how the post-publish gate
proves real greenfield apps work against nuget.org.

## Version SSOT and pins == version

| Location | Role |
|----------|------|
| `source/Directory.Build.props` `<Version>` | Single package version for foundation, analyzers/generators/attributes, modules, identity, and the template |
| Root `Directory.Packages.props` platform `PackageVersion` pins | Must **equal** that same `<Version>` and bump in the **same commit** |

Policy (task 124): packages and the template publish together in one release run, so CPM pins
always reference versions that exist by the time any generated app restores. Do **not** ship a
template whose pins lag the packages it depends on (beta.6 did — fixed by beta.7).

Before tagging:

1. Bump `<Version>` in `source/Directory.Build.props` (and keep the timewarp-templates tree in
   sync if it still carries its own `<Version>`).
2. Bump every platform pin in `Directory.Packages.props` to that same version
   (`TimeWarp.Foundation.*`, `TimeWarp.Modules`, `TimeWarp.Identity`, and the composed
   `$(TwArchitecture*PackageId)` analyzer/generator/attributes pins).
3. `dev check-version` — ensure the version is not already published/tagged
   (`git fetch --tags` first if releases were created via `gh`).
4. `dev build` (0/0) and `dev template-smoke` green on the release branch/PR.

## Automated path (preferred)

1. Merge the version/pin bump to `master`.
2. Create a GitHub Release / tag (prerelease when appropriate). The `release: published` event
   runs `.github/workflows/workflow.yml` → `dotnet run tools/dev-cli/dev.cs -- workflow --api-key …`
   with OIDC Trusted Publishing.
3. Release mode pipeline:

   ```
   clean → build → pack → push → template-publish-smoke
   ```

   - **No test step** on release — tests already gated the PR/merge that produced `master`.
   - **Pack-only** (no API key / no OIDC): pack runs, push is skipped, and
     **template-publish-smoke is skipped** (nothing was published).
   - **Real publish**: after push, `template-publish-smoke` is **required**. Failure sets a
     nonzero exit code and fails the workflow — the release is not done until the gate is green.

## What `template-publish-smoke` does

Command: `dev template-publish-smoke` (`tools/dev-cli/endpoints/template-publish-smoke-command.cs`).

1. Resolves version from `--version` or `source/Directory.Build.props`.
2. Isolates under `artifacts/template-publish-smoke/{cli-home,nuget-packages,work}` via
   `DOTNET_CLI_HOME` + `NUGET_PACKAGES`.
3. Runs an always-on pin-assert self-check (synthetic stale pins must fail).
4. Unless `--skip-wait`: waits (exponential backoff, 12 min budget) for nuget.org
   **flatcontainer** nupkg URLs for the template + all platform packages. Website search-index
   lag is cosmetic — flatcontainer is the restore path.
5. `dotnet new uninstall` / `dotnet new install TimeWarp.Architecture@{version}` in the
   isolated hive.
6. Matrix (same shape as template-smoke): defaults + `--postgres false`, app names ≠ sourceName.
7. Asserts package IDs were not sourceName-rewritten; asserts CPM platform pins == release
   version; writes nuget.org-only `NuGet.config`; `git init` + empty commit (HEAD for
   TimeWarp.Build.Tasks metadata); restore + Release build.

## Standalone command examples

```bash
# Help / compile discovery
dotnet run tools/dev-cli/dev.cs -- template-publish-smoke --help

# Against a known-published version (skip wait when packages are already on flatcontainer)
dotnet run tools/dev-cli/dev.cs -- template-publish-smoke --version 2.0.0-beta.7 --skip-wait

# Full wait (post-publish / first availability)
dotnet run tools/dev-cli/dev.cs -- template-publish-smoke --version 2.0.0-beta.7

# Default version from source/Directory.Build.props
dotnet run tools/dev-cli/dev.cs -- template-publish-smoke
```

## Manual fallback (task 124 proof path)

Use when the automated gate is unavailable or you need to re-prove outside CI:

1. Confirm all platform packages + template exist on nuget.org flatcontainer for the release
   version (not just the website search UI).
2. In a **clean directory outside the monorepo** (no monorepo `NuGet.config`, no local feeds):

   ```bash
   export DOTNET_CLI_HOME="$(pwd)/.cli-home"
   export NUGET_PACKAGES="$(pwd)/.nuget-packages"
   mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"

   dotnet new uninstall TimeWarp.Architecture || true
   dotnet new install TimeWarp.Architecture@2.0.0-beta.7   # use the release version

   dotnet new timewarp-architecture -n ManualPublishProof -o ManualPublishProof --force
   cd ManualPublishProof

   # Confirm Directory.Packages.props platform pins == release version
   # Write NuGet.config with <clear /> + nuget.org only if any other sources leak in

   git init
   git -c user.email=smoke@local -c user.name=smoke commit --allow-empty -m smoke
   dotnet restore
   dotnet build -c Release
   ```

3. Expect **0 Warnings, 0 Errors**. TimeWarp.Build.Tasks git-metadata needs a real HEAD —
   `git init` alone is not enough (empty repo still warns); create an empty commit.

If the gate (or manual proof) fails after packages are already on nuget.org: treat the release
as broken, fix pins/content, bump version, and ship the next beta immediately (same response as
task 124 after beta.6).

## Complementary coverage: `template-smoke` vs `template-publish-smoke`

| | `dev template-smoke` | `dev template-publish-smoke` |
|--|----------------------|------------------------------|
| When | PR / local / branch integrity | After real nuget.org push (release) |
| Packages | Packs this branch at `2.0.0-smoke` into a local feed | Uses **published** nuget.org versions |
| Pins | Rewrites platform CPM pins to smoke version | **Asserts** pins == release version (no rewrite) |
| Failure class | Branch-internal consistency, sourceName rewrite, package-mode layout | Stale/broken published pins, missing packages, real restore path |
| Replaces the other? | No | No |

Keep both. Template-smoke cannot see published-reality bugs by design; publish-smoke is the
gate that would have caught beta.5/beta.6 greenfield breakage.
