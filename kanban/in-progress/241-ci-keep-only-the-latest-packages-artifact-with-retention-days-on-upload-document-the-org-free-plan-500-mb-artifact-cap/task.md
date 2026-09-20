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

- [x] `retention-days` on every upload step; Packages = 3, diagnostics = 1
- [x] releasing guide + tw-release note on the cap and the rerun remedy
- [ ] PR run artifact `expires_at` verified ≈ created_at + 3 days
- [x] `ganda repo audit` — no new failures vs origin/master (pre-existing Errors remain)

## Session

- Created: cockpit (2026-09-20)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementation: grok task-work implementer (2026-09-20)

## Notes

- Sibling repos without retention on their upload step (same fix, separate tasks if wanted):
  timewarp-state, timewarp-mediator. timewarp-ganda and timewarp-nuru already set it.
- GitHub recalculates quota usage every 6–12 h; deletions do not lift the block instantly.
- Memory: cockpit saved `github-org-free-plan-artifact-cap`.

## Results

- `.github/workflows/workflow.yml` "Upload Artifacts" (`Packages-${{ github.run_number }}`) now sets `retention-days: 3`. Naming is unchanged. This workflow has no other `upload-artifact` steps; the YAML comment records `retention-days: 1` for any future diagnostic upload.
- Added maintainer guide `documentation/developer/guides/releasing.md`: Free-plan 500 MB org cap, 3-day Packages retention, and `gh run rerun <ci-run-id>` as the expired/missing-artifact remedy (cross-link to **`tw-release`**, which does not live in this repo).
- `AGENTS.md` Documentation section now names that maintainer guide and lists **`tw-release`** among cross-repo skills. `.template.config/template.json` excludes `documentation/**` so a clone-based `dotnet new` does not ship it (the nupkg already packs only source/tests/msbuild/skills/root files).
- `ganda repo audit`: kebab-path-names and workflow-file pass. Failures match origin/master (`runfile-executable` 26 files, `runfile-shebang` on a done-kanban research runfile, `memsearch-scaffold`, `vscode-window-icon`) plus this worktree missing `bin/dev`. Not auto-fixed: those files are out of this task's scope.
- `expires_at` cannot be confirmed until the PR CI run uploads a `Packages-*` artifact (host `open-pr`). Then query the API as in How to validate.

### How to validate

**Smoke**

1. `rg -n "retention-days|upload-artifact" .github/workflows/workflow.yml` — exactly one upload step, `retention-days: 3`, name still `Packages-${{ github.run_number }}`.
2. `ganda repo audit` — no new failures vs origin/master.
3. After the PR CI `ci` job completes:

```bash
gh api repos/TimeWarpEngineering/timewarp-architecture/actions/artifacts \
  --jq '.artifacts[] | select(.name | startswith("Packages-")) | {name, created_at, expires_at, expired}'
```

**Expect**

- The PR run's `Packages-*` row has `expires_at` ≈ `created_at` + 3 days (GitHub may round to the hour).
- `documentation/developer/guides/releasing.md` states the 500 MB Free-plan cap, 3-day retention, and `gh run rerun <ci-run-id>` as the remedy when cutting more than 3 days after merge.
