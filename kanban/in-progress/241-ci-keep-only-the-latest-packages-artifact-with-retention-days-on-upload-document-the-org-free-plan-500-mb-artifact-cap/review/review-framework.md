# Review framework — task 241

**Date:** 2026-09-20
**Host task:** kanban/in-progress/241-ci-keep-only-the-latest-packages-artifact-with-retention-days-on-upload-document-the-org-free-plan-500-mb-artifact-cap/
**Diff scope:** branch `task/241-ci-keep-only-the-latest-packages-artifact-with-ret` vs `origin/master` (HEAD `5ba9e706`). Product files only: `.github/workflows/workflow.yml`, `documentation/developer/guides/releasing.md`, `AGENTS.md`, `.template.config/template.json`. Exclude `kanban/**`.
**Plan / brief:** kanban/.../task.md — add `retention-days: 3` on the Packages upload (1 on any diagnostic upload), document Free-plan 500 MB cap and `gh run rerun` expired-artifact remedy, do not rename `Packages-${{ github.run_number }}`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a0bcda-bdce-71a2-b8b6-97cbb3e814e1 (2026-09-20)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Brief (what must be true)

- `.github/workflows/workflow.yml` "Upload Artifacts" (`actions/upload-artifact@v4`, name `Packages-${{ github.run_number }}`) has `retention-days: 3`
- Any other upload step in this workflow uses `retention-days: 1` for diagnostics (or there are no other uploads)
- `Packages-${{ github.run_number }}` naming is unchanged
- `documentation/developer/guides/releasing.md` notes the Free-plan 500 MB org cap, 3-day retention, and `gh run rerun <ci-run-id>` as the expired/missing-artifact remedy (cross-link `tw-release`; that skill does not live in this repo)
- Maintainer docs must not ship into generated apps
