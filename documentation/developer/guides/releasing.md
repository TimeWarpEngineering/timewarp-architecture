# Releasing TimeWarp.Architecture

Maintainer guide for cutting this repo's NuGet packages and template. Generated
apps do not ship a `documentation/` tree — this file is for operators of the
template repo itself. Operator sequence lives in the cross-repo **`tw-release`**
skill; this page records **this repo's** version SSOT, workflow, and artifact
retention.

## Version SSOT

`<Version>` in `source/Directory.Build.props`. Keep
`timewarp-templates/Directory.Build.props` `<Version>` and the platform
`PackageVersion` pins in root `Directory.Packages.props` equal to that same
value (task 124). Humans type the version once in the bump PR.

`dev check-version` (git-tag strategy, `.timewarp/dev.jsonc`) must report the
source version newer than the latest GitHub release tag before a cut.

## Normal flow

1. Merge the version-bump PR. Master `push` runs `.github/workflows/workflow.yml`
   (`dev workflow`: clean → build → test). The template project's
   `GeneratePackageOnBuild` writes `.nupkg` files to `artifacts/packages/`.
2. From a **clean, synced master** worktree: `dev release --dry-run`, then
   `dev release`. That tags `v{Version}` and creates the GitHub Release.
3. The `release:published` event runs the same workflow in release mode
   (`dev workflow` with the OIDC NuGet API key): clean → build → pack → push →
   template-publish-smoke. This repo currently **rebuilds and packs** on the
   release run rather than downloading the merge artifact. The merge-run
   `Packages-*` upload is still required for quota-aware CI and for the
   org-wide promotion path documented in **`tw-release`**.

Break-glass and trusted-publishing probe: **`tw-release`**.

## Artifact retention and the org Free-plan cap

TimeWarpEngineering is on GitHub's **Free** plan: **500 MB** of Actions
artifact storage, **shared across the org**. Unexpired `Packages-*` blobs
from this repo and sibling repos have already exhausted that cap
("Artifact storage quota has been hit"). GitHub recalculates usage every
6–12 hours; deletions do not lift the block instantly.

This workflow's `Upload Artifacts` step (`actions/upload-artifact@v4`, name
`Packages-${{ github.run_number }}` — **do not rename**; locate-run matches
that prefix) sets `retention-days: 3`. Three days is the org minimum of 1
plus a weekend between merge and cut. Any future diagnostic upload (smoke
logs, test results) must use `retention-days: 1`.

### Expired or missing `Packages-*` artifact

A release cut more than 3 days after the merge will not find a live
`Packages-*` blob for that commit. Regenerating it is the existing
**`tw-release`** pipeline refusal remedy:

```bash
gh run rerun <ci-run-id>
```

Rerun executes the same commit, then re-run the release workflow. Do not
pack by hand at cut time to "replace" the missing artifact.
