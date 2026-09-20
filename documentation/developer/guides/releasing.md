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
   (`dev workflow`: clean → build → test). That path does **not** pack. The
   `Upload Artifacts` step uses `if-no-files-found: ignore`, so a `Packages-*`
   blob is created only when `artifacts/packages/*.nupkg` already exists.
2. From a **clean, synced master** worktree: `dev release --dry-run`, then
   `dev release`. That tags `v{Version}` and creates the GitHub Release.
3. The `release:published` event runs the same workflow in release mode
   (`dev workflow` with the OIDC NuGet API key): clean → build → **pack** →
   push → template-publish-smoke. Pack writes `.nupkg` files to
   `artifacts/packages/`; the upload step then stores them as
   `Packages-${{ github.run_number }}`. This repo **rebuilds and packs** on
   the release run rather than downloading a merge artifact. The org-wide
   promotion path (locate-run → download-artifact) is documented in
   **`tw-release`**.

Break-glass and trusted-publishing probe: **`tw-release`**.

## Artifact retention and the org Free-plan cap

TimeWarpEngineering is on GitHub's **Free** plan: **500 MB** of Actions
artifact storage, **shared across the org**. Unexpired `Packages-*` blobs
from this repo and sibling repos have already exhausted that cap
("Artifact storage quota has been hit"). GitHub recalculates usage every
6–12 hours; deletions do not lift the block instantly.

This workflow's `Upload Artifacts` step (`actions/upload-artifact@v4`, name
`Packages-${{ github.run_number }}` — **do not rename**; locate-run matches
that prefix) sets `retention-days: 3` (org minimum is 1; 3 leaves a weekend
between merge and cut). Any future diagnostic upload (smoke logs, test
results) must use `retention-days: 1`.

### Expired or missing `Packages-*` artifact

**This repo's** cut does not download a merge `Packages-*`: `release:published`
packs fresh, so cutting more than 3 days after merge is fine here. Rerunning a
**merge** CI run in this repo still would not produce `Packages-*` — PR/merge
is clean → build → test only, and the upload step uses `if-no-files-found:
ignore`.

The org-wide **`tw-release`** locate-run → download-artifact promotion path
(sibling repos, or if this repo later promotes instead of rebuilding) does need
a live `Packages-*`. When that path refuses because the blob expired or is
missing, regenerate it with the existing pipeline refusal remedy:

```bash
gh run rerun <ci-run-id>
```

Rerun executes the same commit, then re-run the release workflow. Do not pack
by hand at cut time to "replace" a missing artifact.
