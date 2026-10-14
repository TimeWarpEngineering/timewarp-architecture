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

- [x] `feature/**` on push + pull_request; `edited` dropped
- [x] Release/publish jobs verified not to fire on feature branches
- [x] Proof: feature-branch PR ran CI; body edit didn't
- [x] Gates: `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, per Steve)
- 2026-10-14: implemented under `ganda task work` (implement oracle, Claude Opus 5.5).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T06:40:32Z
- Review oracle: effort 1, general reviewer (Claude Opus 5.5), session not reported; round 1 clean.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-14T06:41:20Z

## Results

`.github/workflows/workflow.yml`:
- `push.branches` and `pull_request.branches` are `master` + `'feature/**'` (the glob covers
  `feature/overnight`, so the explicit entry is gone). `paths:` filters unchanged.
- `pull_request.types` is `opened, synchronize, reopened` (`edited` removed).
- Header comment records the trigger policy and that publishing is release-only.

### What runs where

| Event | Jobs | Publishes? |
|-------|------|------------|
| `push` to master / `feature/**` | detect-paths; `ci` (if ci paths); template-smoke (if template paths) | No. `dev workflow` auto-detects `push` → Merge mode: clean, build, test, `dev publish compose/kubernetes/aca` (file outputs only). NuGet login step is `release`/probe-only. |
| `pull_request` into master / `feature/**` | detect-paths; `ci`; skill-lint (PR only); template-smoke | No. Pr mode, same as above. |
| `release: published` | `ci` only | Yes: NuGet OIDC login → `dev workflow --api-key` (Release: pack, push, template-publish-smoke). Not branch-scoped by the new globs. |
| `workflow_dispatch` (merge / probe) | `ci` (+ template-smoke unless probe) | probe: OIDC login only. merge: `dev workflow` Release mode with no key → pack-only, no push. Unchanged. |

No job condition keys on branch name, so widening the branch globs cannot reach the publish path;
the publish path is gated on `github.event_name == 'release'` alone.

### Base retarget (`edited`) and the stacked workflow

Retargeting a PR's base fires `edited`; it no longer re-runs CI. `ganda task work-set` opens
nested children with `--base <feature branch>` from the start (`task-work-compute.cs`, "no
retarget is needed"), so the work-set path does not rely on a re-run. The one path that does
retarget is `ganda pr merge --into` on a PR opened against another base: its
`WaitForCiAfterRetargetAsync` waits for a new rollup fingerprint and, with no new run, treats
the stale green as Unknown at timeout and **refuses** the merge (fails safe, not open). Remedy
there: push a commit or close/reopen the PR (`reopened` still triggers). If that path becomes
routine, ganda should re-request checks itself after retarget (ganda follow-up, not this repo).

Side effect: a PR from `feature/x` into master now also gets a `push` run on `feature/x` when
that branch is pushed (same as `feature/overnight` before). Accepted.

### Proof (2026-10-14)

Throwaway branches `feature/359-ci-proof` (from master) and `proof/359-head` (= task commit
e40e81afd), PR #492 into the feature branch:
- `pull_request` into `feature/**` ran: run 38523204068 (created 06:36:30Z).
- PR body edited at 06:36:50Z; 90 s later the branch still had exactly that one run.
- Fast-forwarding `feature/359-ci-proof` to e40e81afd started `push` run 38523400579; its `ci`
  job showed `NuGet login (OIDC Trusted Publishing)=skipped`, `Trusted publishing probe result=skipped`.
- Both runs cancelled and both branches deleted. GitHub marked #492 "merged" because the
  head landed on its throwaway base; nothing reached master (comment on #492 records this).

Gates: `ganda repo audit` passes.

### Review disposition

- Rounds: 1 · effort 1 · roster: general (review oracle, Claude Opus 5.5)
- Final counts: bug 0 · suggestion 0 · nit 0 (0 open / 0 fixed / 0 wontfix)
- Disposition: **clean**
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke:**
```bash
git push origin origin/master:refs/heads/feature/ci-check
git push origin HEAD:refs/heads/proof/ci-check-head   # any branch whose diff touches a CI path
gh pr create --base feature/ci-check --head proof/ci-check-head --title "throwaway" --body "x"
gh run list --workflow workflow.yml --branch proof/ci-check-head    # one pull_request run
gh pr edit <n> --body "edited"; gh run list --workflow workflow.yml --branch proof/ci-check-head
# clean up: gh run cancel, gh pr close, git push origin --delete feature/ci-check proof/ci-check-head
```

**Expect:** the PR into `feature/ci-check` starts one `pull_request` run; after the body edit the
run count stays at one. In any push/PR run, the `ci` job's `NuGet login` step is `skipped`.
