# Releasing TimeWarp.Architecture

Maintainer guide for cutting this repo's NuGet packages and template. Generated
apps do not ship a `documentation/` tree — this file is for operators of the
template repo itself. Operator sequence lives in the cross-repo **`tw-release`**
skill; this page records **this repo's** version SSOT, packable set, workflow,
and artifact retention.

## Version SSOT

`<Version>` in `source/Directory.Build.props`. Keep
`timewarp-templates/Directory.Build.props` `<Version>` and the platform
`PackageVersion` pins in root `Directory.Packages.props` equal to that same
value (task 124). Humans type the version once in the bump PR.

`dev check-version` must report the source version as not yet on nuget.org
before a cut.

## Packable set

Twelve packages, one version. `dev check-version`, `dev release`, the master
pack, and the release job's verify and push all read the same set from
`ArchitecturePackableProjectService` (`tools/dev-cli/services/`): MSBuild
`IsPackable` and `PackageId` of every project under `source/` (11 packages)
plus `timewarp-templates/source/timewarp-architecture-template` (the
`TimeWarp.Architecture` template). No file lists the projects by hand. A new
packable project under `source/` joins the set by itself.

## Normal flow

The packages are built and tested once, on the master merge. The release
promotes those bytes; it never rebuilds.

1. Merge the version-bump PR. The master `push` runs
   `.github/workflows/workflow.yml`. The run ends with a
   `Packages-${{ github.run_number }}` artifact holding the 12 `.nupkg` files
   (and their `.snupkg` symbols):
   - When `ci-group` runs, `ci (publish)` packs and verifies the set after
     `publish all` (`dev workflow --phase publish --pack`).
   - When `ci-group` is off — a green-key hit (the usual case: the merge of a
     pull request whose CI already passed on the same tree) or a skills-only
     push (the template package still changes) — the `pack-only` job runs
     `dev workflow --phase pack`. It carries forward a live `Packages-*` of an
     earlier non-pull-request run with the same green key when classify found
     one, else it builds and packs this commit. It runs no tests and writes no
     green-key marker.
   - Either job uploads `staged-packages`. The `ci` job publishes it as
     `Packages-<run_number>` only after every gate passed, deletes `Packages-*`
     older than the two newest, and deletes `staged-packages`.

   A pull request never uploads `Packages-*`.
2. From a **clean, synced master** worktree: `dev release --dry-run`, then
   `dev release`. It walks back past kanban-only commits to the newest commit
   whose successful run has a live `Packages-*`, checks its attestation, tags
   `v{Version}` there, and creates the GitHub Release.
3. The `release:published` event runs the `release` job (`dev workflow` in
   release mode, with the OIDC NuGet API key):
   1. **tag-gate** — the tag is `v{Version}`; when `v{Version}` already exists
      it points at HEAD; HEAD is an ancestor of `origin/master`; the
      ganda-audit attestation on HEAD's tree is valid (mode `require` unless
      `.timewarp/dev.jsonc` `attestation.mode` or `--attestation` says otherwise).
   2. **check-version** — nuget.org does not already have the version in full.
   3. **locate-run** — the successful `workflow.yml` runs at HEAD
      (`pull_request` runs never count; `push` runs first).
   4. **download-artifact** — the first candidate's live `Packages-*`.
   5. **verify** — exactly one `{PackageId}.{Version}.nupkg` per packable
      project, nothing else.
   6. **push** — `dotnet nuget push --skip-duplicate` per package.

   Then `template-publish-smoke` generates an app from the published template
   and builds it against nuget.org only. Its failure fails the release run.

The PR/merge classify job verifies the attestation too, as an advisory
(mode `warn`).

## Trusted-publishing probe and break-glass

- Probe: dispatch `workflow.yml` with `mode: probe`. It runs only the OIDC
  login.
- Break-glass: dispatch on the tag (or master) with `mode: release` and
  `confirm: release`. The job runs the same six steps with `--mode release`;
  the tag assertion is skipped (no release ref), the tag pin is not. Use it to
  resume a partial publish (`--skip-duplicate` makes a re-push safe) or when
  the release event did not fire. A dispatch with `mode: release` and any other
  `confirm` refuses. See **`tw-release`**.

## Artifact retention and the org Free-plan cap

TimeWarpEngineering is on GitHub's **Free** plan: **500 MB** of Actions
artifact storage, **shared across the org**. GitHub recalculates usage every
6–12 hours; deletions do not lift the block instantly.

`Packages-${{ github.run_number }}` (**do not rename**; locate-run and
`dev release` match that prefix) and `staged-packages` use
`retention-days: 3` (a weekend between merge and cut), and the `ci` job keeps
only the two newest `Packages-*`. Any future diagnostic upload (smoke logs,
test results) must use `retention-days: 1`.

### Expired or missing `Packages-*` artifact

`dev release` and the release job refuse when the commit's `Packages-*` has
expired or was never uploaded. Regenerate it from the same commit:

```bash
gh run rerun <ci-run-id>
```

A master commit with no run at all (a push the `paths` filter skips, such as a
documentation-only merge, when `dev release` cannot walk past it) gets one
from a dispatch on master with `mode: full`. Do not pack by hand at cut time to
"replace" a missing artifact.
