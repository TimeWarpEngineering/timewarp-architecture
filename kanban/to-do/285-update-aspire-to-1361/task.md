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

- [ ] CPM Aspire pins + AppHost SDK + CI `Aspire.Cli` → 13.6.1 (previews to their 13.6.1 builds)
- [ ] Release notes read; any workaround simplification recorded
- [ ] Design-region version wording reconciled
- [ ] Gates: `dev clean` then `dev build` 0/0 (stale wasm/webcil after package bumps), `dev test`,
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
