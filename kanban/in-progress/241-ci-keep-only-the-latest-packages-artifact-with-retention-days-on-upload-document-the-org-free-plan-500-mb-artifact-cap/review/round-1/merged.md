# Round 1 — merged findings
**Date:** 2026-09-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: documentation/developer/guides/releasing.md:54
- Description: After the pack-path correction, Normal flow correctly states PR/merge `dev workflow` does not pack and this repo **rebuilds and packs** on `release:published` rather than downloading a merge `Packages-*`. The “Expired or missing `Packages-*` artifact” subsection still says a cut more than 3 days after merge “will not find a live `Packages-*` blob for that commit” and that `gh run rerun <ci-run-id>` then re-running release is the remedy. For this repo that framing is wrong: merge typically never uploads `Packages-*` (`if-no-files-found: ignore`; `WorkflowCommand.RunPrAsync` is clean → build → test only), and the cut does not depend on a merge artifact. The rerun remedy belongs to the org-wide **`tw-release`** locate-run → download-artifact path (and sibling repos that promote merge artifacts), not as a prerequisite for this repo’s `dev release` → rebuild path. Task still requires documenting the remedy; presence is fine, scope is not.
- Suggestion: Scope the subsection to the org-wide / `tw-release` locate-run path (or “when following locate-run rather than this repo’s rebuild-on-release cut”). Note that for this repo, cutting more than 3 days after merge is fine because `release:published` packs fresh; `gh run rerun` of a merge CI run here still would not produce `Packages-*`. Keep the Free-plan / 3-day retention prose and the cross-link.
- Source: general
- Disposition notes: Fixed in `33b35648`. Subsection now states this repo packs on `release:published` (merge rerun would not produce nupkgs) and scopes `gh run rerun` to the org-wide locate-run path.

## Duplicates / conflicts

- None (single reviewer).
