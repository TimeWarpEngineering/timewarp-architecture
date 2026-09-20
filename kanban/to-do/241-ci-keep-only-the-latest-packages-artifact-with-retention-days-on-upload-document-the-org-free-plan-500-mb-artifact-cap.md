# CI: keep only the latest Packages artifact with retention-days on upload; document the org Free-plan 500 MB artifact cap

## Description

2026-09-20: the TimeWarpEngineering org is on GitHub's Free plan (500 MB shared Actions artifact
storage). Unexpired artifacts reached 0.53 GB (netclaw fork 338 MB, this repo 101 MB across 18
`Packages-*` artifacts dating back to June) and every upload in the org failed with
"Artifact storage quota has been hit". The cockpit disabled Actions on the netclaw fork and
deleted its artifacts, and deleted this repo's backlog. Steve: keep nothing but the latest;
set retention so the cap stays out of reach.

The release pipeline (`dev release` → `release:published` run → tag-gate → check-version →
locate-run → download-artifact → verify → push) only needs the `Packages-*` artifact of the CI
run at the tagged commit (HEAD at cut time). Break-glass resume regenerates it with
`gh run rerun <ci-run-id>` (documented in `tw-release`). So a short retention is safe.

## Requirements

- `.github/workflows/workflow.yml` "Upload Artifacts" step (`actions/upload-artifact@v4`,
  `Packages-${{ github.run_number }}`): add `retention-days: 3` (org minimum is 1; 3 leaves a
  weekend between merge and cut). Same for any other upload step in the workflow (smoke logs,
  test results) — use `retention-days: 1` for diagnostics.
- `documentation/developer/guides/releasing.md` (and the `tw-release` skill if it lives in
  this repo): note the Free-plan cap, the 3-day artifact retention, and that a release cut more
  than 3 days after the merge needs `gh run rerun <ci-run-id>` first (the pipeline's
  "expired/missing artifact" remedy already exists — cross-link it).
- Do **not** change `Packages-${{ github.run_number }}` naming; `locate-run` depends on it.
- Verify: push the PR, confirm the PR run's artifact shows the 3-day expiry in
  `gh api repos/TimeWarpEngineering/timewarp-architecture/actions/artifacts` (`expires_at`).

## Checklist

- [ ] `retention-days` on every upload step; Packages = 3, diagnostics = 1
- [ ] releasing guide + tw-release note on the cap and the rerun remedy
- [ ] PR run artifact `expires_at` verified ≈ created_at + 3 days
- [ ] `ganda repo audit` clean

## Session

- Created: cockpit (2026-09-20)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Sibling repos without retention on their upload step (same fix, separate tasks if wanted):
  timewarp-state, timewarp-mediator. timewarp-ganda and timewarp-nuru already set it.
- GitHub recalculates quota usage every 6–12 h; deletions do not lift the block instantly.
- Memory: cockpit saved `github-org-free-plan-artifact-cap`.

## Results

_Pending._

### How to validate

_Pending._
