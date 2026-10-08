# Update Aspire to 13.6.1

## Description

Aspire 13.6.1 is on nuget.org; I checked the flat container on 2026-10-08. Move every Aspire pin
in the repo from 13.6.0 to 13.6.1 in one change. The template ships these pins to every generated
app.

Versions available on nuget.org:
- **Stable:** `Aspire.Hosting.*`, `Aspire.Cli`, `Aspire.Hosting.Azure.AppContainers` →
  **13.6.1**.
- **Preview:** `Aspire.Hosting.Kubernetes` → **13.6.1-preview.1.26506.6**. For
  `Aspire.Hosting.EntityFrameworkCore`, use the matching 13.6.1 preview build. Check the flat
  container for the exact version; don't guess.

## Requirements

- `Directory.Packages.props`: every `Aspire.*` `PackageVersion` moves to its 13.6.1 version. That
  covers Yarp, PostgreSQL, Docker, Azure.AppContainers, Azure.PostgreSQL, Testing and the AppHost
  group, plus the two previews above. If a package has no 13.6.1 build, leave it on 13.6.0 and say
  so in the PR.
- AppHost SDK: `aspire-app-host.csproj` `Sdk="Aspire.AppHost.Sdk/13.6.1"`.
- CI: in `.github/workflows/workflow.yml`, `dotnet tool install --global Aspire.Cli --version 13.6.1`.
- Leave the minimum-CLI-version guards in the dev CLI as they are. Those are the 13.6 *floors* in
  `aspire-run.cs`, `db-nuke`, `aspire-publish` and `aspire-deploy`: 13.6.1 satisfies them, so they
  are not pins. Only update a test that hard-codes `13.6.0` as the version it parses or prints, and
  only if it then fails.
- Check `program.cs` and the other Design regions that cite "13.6.0" behaviour. Update the wording
  only where it names a pinned version, not where it records which version introduced a behaviour.
- Read the 13.6.1 release notes (github.com/microsoft/aspire releases). If a fix touches something
  we worked around, simplify the workaround and say so. For example:
  - ASPIRE010 / `AspireUseCliBundle`;
  - the ACA https-upgrade redirect;
  - the Kubernetes or EF previews;
  - the compose dashboard.

  Don't simplify anything on a guess.
- Ownership: the platform CPM pins equal `<Version>` (124 policy) and are not Aspire. Run
  `dev check-version` only if this PR also bumps the release version; a dependency bump alone does
  not need it.

## Checklist

- [x] CPM Aspire pins + AppHost SDK + CI `Aspire.Cli` → 13.6.1 (previews to their 13.6.1 builds)
- [x] Release notes read; any workaround simplification recorded
- [x] Design-region version wording reconciled
- [x] Gates: `dev clean` then `dev build` 0/0 (stale wasm/webcil after package bumps), `dev test`,
      `dev publish compose`, `dev publish kubernetes`, `dev publish aca`, `dev template-smoke`,
      `ganda repo audit`

## Notes

- Workers never start the maintainer's AppHost. aspire-tests boots its own test AppHosts, which is
  fine.
- The local Aspire CLI is 13.6.0, which is fine for the worker's gates. The maintainer updates their
  CLI with `aspire update --self` after merge.
- After merging, clear site data in the browser if the SPA throws TypeLoad or TypeInitialization
  errors. Any package bump can leave stale wasm.

## Session

- Created: 2026-10-08 (cockpit, per Steve)
- 2026-10-08 implementer (ganda task work): pins bumped, release notes read, gates green.

## Results

**Pins (all checked against the nuget.org flat container on 2026-10-08; every package has a 13.6.1 build):**

- `Directory.Packages.props`: `Aspire.Hosting.Yarp`, `.PostgreSQL`, `.Docker`, `.Azure.AppContainers`,
  `.Azure.PostgreSQL`, `.Testing` → `13.6.1`; `Aspire.Hosting.Kubernetes` and
  `Aspire.Hosting.EntityFrameworkCore` → `13.6.1-preview.1.26506.6` (the same preview build for both).
  The two preview comments now say "Same train as stable 13.6.1".
- `aspire-app-host.csproj`: `Sdk="Aspire.AppHost.Sdk/13.6.1"`. This brings `Aspire.Hosting.AppHost`
  13.6.1 transitively (checked in `project.assets.json`). The "Aspire AppHost" group in CPM has
  no Aspire pin; it only holds MessagePack.
- `.github/workflows/workflow.yml`: `Aspire.Cli --version 13.6.1`.

**Left unchanged:** the dev-cli minimum-version floors are not pins. The `aspire-run-tests` and
`db-nuke-tests` fixtures that parse the string `13.6.0+…` still pass (118/118). The `program.cs`
Design region ("Re-tested 2026-10-02 on Aspire 13.6.0 …") and the `run-command.cs` note record
what was verified on which version, so they keep 13.6.0.

**Release notes (microsoft/aspire v13.6.1):** the fixes are dashboard metric-retention CPU, Metrics
tree scrolling, dashboard navigation after a disconnect, DCP ContainerExec watch retry, and
Windows session-container cleanup. None of them touches ASPIRE010 / `AspireUseCliBundle`, the
ACA https-upgrade redirect, the Kubernetes or EF previews, the compose dashboard, or the
web-migrations wait-edge issue. **No workaround simplified.**

**Gates (worktree, 2026-10-08):** `dev clean` ✓; `dev build` 0 warnings / 0 errors; `dev test`
passed every suite, 0 failures (aspire-tests 38/38, web-server-integration 295/295,
web-jaribu 227/227, …); `dev publish compose` / `kubernetes` / `aca` all production-safe
(helm lint 0 failed); `dev template-smoke` SUCCEEDED; `ganda repo audit` clean. The publishes ran
with Aspire CLI 13.6.1 (`~/.dotnet/tools/aspire`). No release-version bump, so
`dev check-version` was not needed.

### How to validate

**Smoke:**
```bash
grep -n 'Aspire\.' Directory.Packages.props | grep Version
grep -n 'Aspire.AppHost.Sdk' source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
grep -n 'Aspire.Cli --version' .github/workflows/workflow.yml
dev clean && dev build && dev test
dev publish compose && dev publish kubernetes && dev publish aca
```

**Expect:** every `Aspire.*` pin is `13.6.1` (Kubernetes and EntityFrameworkCore are
`13.6.1-preview.1.26506.6`); the SDK and CI pins are `13.6.1`; the build has 0 warnings and
0 errors; every test suite passes; each publish prints "production-safe". After pulling, clear
the browser's site data if the SPA throws TypeLoad or TypeInitialization errors.
