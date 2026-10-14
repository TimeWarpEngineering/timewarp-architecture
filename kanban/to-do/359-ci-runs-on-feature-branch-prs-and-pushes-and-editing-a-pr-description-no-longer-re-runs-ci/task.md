# CI runs on feature-branch PRs and pushes, and editing a PR description no longer re-runs CI

## Problem (2026-10-14)

`.github/workflows/workflow.yml` triggers:
- `push.branches` = `master`, `feature/overnight`;
- `pull_request.branches` = `master`, `feature/overnight`;
- `pull_request.types` includes **`edited`**.

Consequences seen this week:
1. **Stacked child PRs never ran CI.** `ganda task work-set` merges each child PR onto its feature
   branch (`feature/ask-panel`: #482–#486; `feature/ask-composer`: #488–#490). None ran CI because
   their base wasn't master. Breakage only appeared on the final master PR:
   - #487: the flaky Tag Playwright test (task 351);
   - #491: the settings-menu Escape bug.
2. **Editing a PR description re-runs the full pipeline** (`edited`). On #487 that extra run is what
   hit the Tag flake. A description edit changes no code.

## Requirements

- `pull_request.branches` and `push.branches`: add **`feature/**`**. Keep `master` and the existing
  `feature/overnight` (covered by the glob; simplify if equivalent). The `paths:` filters stay.
- Remove **`edited`** from `pull_request.types` (keep opened, synchronize, reopened). Retargeting a PR's
  base also fires `edited`. Make sure the stacked workflow doesn't rely on a re-run after a base
  change, and record that.
- The release and other mode-specific jobs (`dev workflow` modes, release/publish on
  `release: published`) must **not** start publishing for feature branches. Verify each job's
  conditions and record what runs where.
- Branch protection, if any, isn't changed here.
- **Proof:** open a throwaway PR into a `feature/…` branch, or use the next work-set, and show CI ran
  on it. Show that editing a PR body no longer starts a run.

## Coordinates with

- **ganda 394:** `ganda pr merge --into` treats an empty check rollup as green. With this task,
  child PRs get real checks, and ganda 394 makes the merge actually wait for them.

## Checklist

- [ ] `feature/**` on push + pull_request; `edited` dropped
- [ ] Release/publish jobs verified not to fire on feature branches
- [ ] Proof: feature-branch PR ran CI; body edit didn't
- [ ] Gates: `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve)
