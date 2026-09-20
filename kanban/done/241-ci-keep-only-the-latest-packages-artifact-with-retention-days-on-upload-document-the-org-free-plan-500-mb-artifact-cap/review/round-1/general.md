# Round 1 — general
**Date:** 2026-09-20
**Scope reviewed:** `.github/workflows/workflow.yml`, `documentation/developer/guides/releasing.md`, `AGENTS.md`, `.template.config/template.json` (`origin/master...HEAD` `5ba9e706`, exclude `kanban/**`)

## Summary

The change sets `retention-days: 3` on the sole `Packages-${{ github.run_number }}` upload (name unchanged; no other `upload-artifact` steps), documents the Free-plan 500 MB org cap and `gh run rerun` remedy, and keeps maintainer docs out of generated apps via `documentation/**` template exclude plus AGENTS.md wording. Risk is low: CI YAML and packing exclusions look correct. Dominant theme is a remaining docs framing mismatch in the expired-artifact subsection versus this repo’s rebuild-on-release path.

## Issues

### Issue 1 — Severity: suggestion
- File: documentation/developer/guides/releasing.md:54
- Description: After the pack-path correction, Normal flow correctly states PR/merge `dev workflow` does not pack and this repo **rebuilds and packs** on `release:published` rather than downloading a merge `Packages-*`. The “Expired or missing `Packages-*` artifact” subsection still says a cut more than 3 days after merge “will not find a live `Packages-*` blob for that commit” and that `gh run rerun <ci-run-id>` then re-running release is the remedy. For this repo that framing is wrong: merge typically never uploads `Packages-*` (`if-no-files-found: ignore`; `WorkflowCommand.RunPrAsync` is clean → build → test only), and the cut does not depend on a merge artifact. The rerun remedy belongs to the org-wide **`tw-release`** locate-run → download-artifact path (and sibling repos that promote merge artifacts), not as a prerequisite for this repo’s `dev release` → rebuild path. Task still requires documenting the remedy; presence is fine, scope is not.
- Suggestion: Scope the subsection to the org-wide / `tw-release` locate-run path (or “when following locate-run rather than this repo’s rebuild-on-release cut”). Note that for this repo, cutting more than 3 days after merge is fine because `release:published` packs fresh; `gh run rerun` of a merge CI run here still would not produce `Packages-*`. Keep the Free-plan / 3-day retention prose and the cross-link.
- Status: open
