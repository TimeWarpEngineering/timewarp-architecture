# CI caches NuGet packages and uses shallow checkout where full history is not needed

## Why
Baseline (2026-10-15, run starting 07:17): `ci` job ~14 min = runfile compile 43s + `dev build` 89s + `dev test` ~9m45s (25 projects, one at a time) + 3 publishes ~2m. `template-smoke` ~6.5 min runs in parallel. Part of the CI speed-up stack 365 → 366 → 367 → 368 → 369 → 370.

- **Checkout:** `actions/checkout` uses `fetch-depth: 0` (workflow.yml lines ~86 and ~149), which
  pulls the full history and every old branch (the log lists `Cramer/2019-…` branches).
- **NuGet:** restore runs cold on every run, with no NuGet package cache.

## Requirements
- Use **shallow checkout** (`fetch-depth: 1`) in jobs that don't need history.
  - Keep full history only where versioning, SourceLink or `check-version` need tags or history,
    and record which jobs.
  - Never break `dev check-version` or SourceLink.
- Cache NuGet packages, either `actions/setup-dotnet` with `cache: true` keyed on
  `Directory.Packages.props` / lock inputs, or `actions/cache` on `~/.nuget/packages`, in
  every job that restores. Make sure a cache can't serve stale prerelease bits. The key must
  include the CPM file hash. (#647 was a local-cache poisoning issue; CI keys by content hash.)
- Optionally cache the dev CLI runfile build output if it's safe (key: `tools/dev-cli/**` plus SDK
  version). Record the decision.
- Before/after setup and restore timing in the PR.

## Checklist
- [x] Shallow checkout where safe; full history kept and justified where needed
- [x] NuGet cache keyed on CPM hash in every restoring job
- [x] Runfile cache decision recorded
- [ ] Gates: `ganda repo audit`; CI green on the PR

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
- 2026-10-15: implemented in `.github/workflows/workflow.yml` (implementer, ganda task work).

## Results

All changes are in `.github/workflows/workflow.yml`. The header comment records the decisions.

**Shallow checkout: every job uses `fetch-depth: 1`. No job keeps full history.**
- Nothing in CI reads history or tags:
  - SourceLink (built into the SDK) needs only HEAD and the remote URL.
  - `TimeWarp.Build.Tasks` `GitMetadataTask` runs only `git rev-parse HEAD` and `git log -1 --format=%ct`
    (checked in the 1.0.0 package dll).
  - `dev check-version` compares `<Version>` with nuget.org, and CI doesn't run it.
  - `dev release` is the only command that writes tags. It runs locally, not in CI.
  - `template-publish-smoke` runs `git init` in its own output dir.
- `detect-paths` needed history only for `git diff BASE HEAD`. It now runs
  `git fetch --no-tags --depth=1 origin $BASE $HEAD`. A two-commit tree diff needs nothing in
  between. If the base can't be fetched (for example after a force push), the job takes the same
  enable-all path as an unknown base.
- `release` is shallow too. It packs from HEAD, so it has the same SourceLink and metadata
  inputs.

**NuGet cache: `actions/cache@v4` on `~/.nuget/packages`.**
- Key: `nuget-<os>-<lane>-hashFiles('**/Directory.Packages.props', 'msbuild/*.props', 'global.json')`.
  - It includes the CPM hash: the root file and the `timewarp-templates` copy.
  - There are no `restore-keys`, so a CPM change starts a clean cache instead of layering onto a
    stale one.
- `ci-group` (all 4 matrix jobs): one shared `ci` key. Each job builds the whole solution, so
  they restore the same package set. The first job to finish saves the cache.
- `template-smoke`: its own key, because the generated app restores a different set.
  - It packs this branch's platform packages as `2.0.0-smoke`. Those land in the global cache,
    which is the #647-style poisoning vector.
  - A final `if: always()` step runs `rm -rf ~/.nuget/packages/*/2.0.0-smoke` before the cache
    post-step saves.
  - `PurgeStaleSmokeCacheEntries` also evicts them before restore (defence in depth).
- `release`: `actions/cache/restore@v4` only (read-only, reuses the `ci` key). A release run never
  seeds a cache. `template-publish-smoke` already uses an isolated `NUGET_PACKAGES`.
- `detect-paths` and `skill-lint` don't restore, so they have no cache.
- `setup-dotnet` `cache: true` wasn't used. It keys on `packages.lock.json`, and this repo has
  no lock files.

**Runfile cache: declined.**
- `dotnet run tools/dev-cli/dev.cs` decides whether its build is up to date by comparing file
  times. Checkout writes fresh times on every run, so a restored build output would be rebuilt
  anyway, or would need fragile mtime tricks.
- The runfile's package restore is already served by the NuGet cache. The compile step (~43s)
  stays.

**Before / after timing**
- Before: master run 38699376705 (2026-10-15, before the task 366 split).
  - Checkout: 2–3s per job with `fetch-depth: 0` (`detect-paths` checkout ~4s).
  - Setup .NET: 8–9s.
  - NuGet restore, cold. Summed per-project `Restored … (in …)` times, which overlap inside a
    restore, so this is not wall time:
    - `ci`: 51 restores, 114s
    - `template-smoke`: 93 restores, 44s
- After: read the same step and restore lines on this PR's CI.
  - The first run on a new CPM hash is a cache miss and saves the cache.
  - Re-run the PR (`gh run rerun <id>`) to measure the warm cache. `Cache NuGet packages` should
    log `Cache restored from key: nuget-Linux-ci-…`.

### How to validate

**Smoke:**
1. Open this PR and let CI run. On the `ci (fast|playwright|integration|publish)` and
   `template-smoke` jobs, check that the `Cache NuGet packages` step reports a miss and the post
   step reports `Cache saved`, with exactly one save for the `ci` key.
2. Run `gh run rerun <run-id>`. In that run, the cache step logs `Cache restored from key:
   nuget-Linux-ci-<hash>`, and the `Restored … (in …)` times in `Run CI Pipeline` drop.
3. In the `detect-paths` log, the checkout fetches with `--depth=1`. `Classify changed paths`
   fetches the base and head SHAs and prints the changed-file list (not the
   `Could not fetch base` fallback).
4. In the `template-smoke` log, the `Evict smoke packages from NuGet cache` step runs before
   `Post Cache NuGet packages`.

**Expect:**
- All CI jobs are green with shallow checkouts.
- Path gating still classifies correctly.
- The second run gets NuGet cache hits for both the `ci` and `template-smoke` keys.
- No cache contains `*/2.0.0-smoke`.
- The `release` job is unchanged except for the shallow checkout and the read-only cache restore.
