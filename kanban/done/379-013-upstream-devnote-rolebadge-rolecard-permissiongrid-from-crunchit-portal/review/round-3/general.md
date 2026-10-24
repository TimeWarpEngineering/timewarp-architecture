# Round 3 — general
**Date:** 2026-10-24
**Scope reviewed:** merge `d94228528` (`feature/379-portal-rest` into the task branch) and the branch delta that PR #568 now carries, `origin/feature/379-portal-rest..HEAD` (18 product files, +952 / −8). The full `master...HEAD` diff (1302 lines) also includes the 379-012 commits. Those were reviewed on 379-012 and are not findings here.

## Summary

Round 2 closed with a clean disposition. Then the task branch merged `feature/379-portal-rest`, which holds 379-012 (ChartCard, ChartLegendItem, TrendPill, SlaChip, ClassPill, and the `Error` badge status). PR #568 targets that feature branch. Four files had conflicts: `StatusBadge.razor`, `components/overview.md`, `StyleGuidePage.razor.css`, and `tokens.css`. The combined diff (`git show --cc`) shows each one kept both sides, and nothing from either side was lost or duplicated.

- `StatusBadge.razor`: the merged Design region names both the unmatched-attribute splat (379-013) and the `Error` → `twe-badge--error` mapping (379-012). The code has `AdditionalAttributes` and the `Error` arm. `RoleBadge` passes `Status` through, so a role badge can use `Error` as well.
- `overview.md`: the StatusBadge row names the five statuses and the label and attribute passthrough. The 379-012 rows (ChartCard, ChartLegendItem, TrendPill, SlaChip, ClassPill) and the 379-013 rows (RoleBadge, RoleCard, PermissionGrid, DevNote) are each listed once.
- `StyleGuidePage.razor.css`: `.sg-follow` and `.sg-choice-grid` (379-013) and the `.sg-chart-sample*` rules (379-012) are all there, with no duplicates.
- `tokens.css`: both sides defined `--twe-on-positive: #14784a`, and there is now one definition. `--twe-on-warning: #92400e` comes from 379-012. The merged comment says these clear 4.5:1 on paper. Recomputed against `--twe-paper` `#ffffff`, the ratios are 5.50:1 and 7.09:1, so the claim holds. `PermissionGrid`, `TrendPill`, and `SlaChip` use `--twe-on-positive`.

Measured against the branch it now targets, the PR is exactly the 379-013 scope: the four components, `PermissionGroup` / `PermissionItem`, the StatusBadge splat, the token comment, the style-guide samples and CSS, the overview rows, and four render-test files. The goal restatement matches the original task, so no misread is recorded.

Re-verified in this worktree on `d94228528`:

- `./bin/dev build`: 0 warnings, 0 errors.
- HtmlRenderer classes all passed. From 379-013: StatusBadge 3/3, RoleBadge 5/5, RoleCard 4/4, PermissionGrid 5/5, DevNote 4/4. From 379-012: ChartCard 5/5, SlaChip 5/5, TrendPill 4/4, ClassPill 4/4, ChartLegendItem 4/4.
- `git diff origin/master...HEAD -- source tests | grep -ic crunchit` → 0.
- GitHub `CI/CD` on head `d94228528`: success.

`template-smoke` was not re-run locally. The CI run on the same head includes it.

## Issues

None.
