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
- Implementer re-entry (ganda task-work): grok (2026-09-20) — remaining product: correct the releasing-guide pack path; refresh Results

## Notes

- Sibling repos without retention on their upload step (same fix, separate tasks if wanted):
  timewarp-state, timewarp-mediator. timewarp-ganda and timewarp-nuru already set it.
- GitHub recalculates quota usage every 6–12 h; deletions do not lift the block instantly.
- Memory: cockpit saved `github-org-free-plan-artifact-cap`.

## Results

- `.github/workflows/workflow.yml` "Upload Artifacts" (`Packages-${{ github.run_number }}`) sets `retention-days: 3`. Naming is unchanged. This workflow has no other `upload-artifact` steps; the YAML comment records `retention-days: 1` for any future diagnostic upload.
- Maintainer guide `documentation/developer/guides/releasing.md`: Free-plan 500 MB org cap, 3-day Packages retention, and `gh run rerun <ci-run-id>` as the expired/missing-artifact remedy (cross-link to **`tw-release`**, which does not live in this repo). The guide states that PR/merge `dev workflow` is clean → build → test (no pack); a `Packages-*` blob is created only when `artifacts/packages/*.nupkg` exists (release-mode pack).
- `AGENTS.md` Documentation section names that maintainer guide and lists **`tw-release`** among cross-repo skills. `.template.config/template.json` excludes `documentation/**` so a clone-based `dotnet new` does not ship it (the nupkg already packs only source/tests/msbuild/skills/root files).
- `ganda repo audit` (this worktree): kebab-path-names and workflow-file pass. Failures vs origin/master: same `runfile-executable` (26), `runfile-shebang` (done-kanban research runfile), `memsearch-scaffold`, `vscode-window-icon`, plus this worktree missing `bin/dev` (master worktree has it). Not auto-fixed: out of this task's scope.
- `expires_at` cannot be confirmed until a CI run actually uploads a `Packages-*` artifact (host `open-pr`). PR/merge may no-op the upload (`if-no-files-found: ignore`). Then query the API as in How to validate.

### How to validate

**Smoke**

1. From the task worktree:

```bash
rg -n "retention-days|upload-artifact|Packages-" .github/workflows/workflow.yml
```

2. `ganda repo audit` — no new failures vs origin/master besides this worktree missing `bin/dev`.

3. After host `open-pr`, when the PR `ci` job finishes:

```bash
gh api repos/TimeWarpEngineering/timewarp-architecture/actions/artifacts \
  --jq '.artifacts[] | select(.name | startswith("Packages-")) | {name, created_at, expires_at, expired}'
```

**Expect**

- Exactly one `upload-artifact` step; `retention-days: 3`; name still `Packages-${{ github.run_number }}`.
- `documentation/developer/guides/releasing.md` states the 500 MB Free-plan cap, 3-day retention, and `gh run rerun <ci-run-id>` as the remedy when cutting more than 3 days after merge.
- If a `Packages-*` row exists for that run: `expires_at` ≈ `created_at` + 3 days (GitHub may round to the hour). If the upload no-ops because no nupkgs were packed, there is no row; the same `retention-days: 3` applies on the next release-mode pack that does upload.
