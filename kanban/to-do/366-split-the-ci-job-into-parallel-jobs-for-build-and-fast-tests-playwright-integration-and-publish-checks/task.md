# Split the CI job into parallel jobs: build + fast tests, Playwright, integration, publish checks

## Why
Baseline (2026-10-15, run starting 07:17): `ci` job ~14 min = runfile compile 43s + `dev build` 89s + `dev test` ~9m45s (25 projects, one at a time) + 3 publishes ~2m. `template-smoke` ~6.5 min runs in parallel. Part of the CI speed-up stack 365 → 366 → 367 → 368 → 369 → 370.

Everything runs one step after another in the single `ci` job. The wall clock is the **sum**
of phases; it should be the **max**.

## Requirements
- Split `.github/workflows/workflow.yml`'s `ci` job into parallel jobs (names are a suggestion):
  1. **build + unit/fast tests:** analyzers, sourcegen, foundation, libraries, contracts/domain,
     dev-cli and agent-identity tests;
  2. **playwright:** `web-spa-playwright-tests`, uploading screenshots as today;
  3. **integration:** aspire-tests, `*-integration-tests`, web-infrastructure (Testcontainers),
     jaribu aggregators;
  4. **publish checks:** `dev publish compose|kubernetes|aca`, uploading artifacts as today.
- Each job either builds what it needs or downloads a shared build artifact from a single build job.
  Measure both and record the choice.
- **`dev workflow` modes stay the single source of truth** (the CI pattern: mode-aware
  `dev workflow`). Add a way to run a subset, for example `dev workflow --phase test --group
  playwright` or `dev test --group <name>`, with groups defined in one place in the dev CLI. Avoid
  duplicating project lists in YAML. Prefer deriving groups by convention (folder or project-name
  patterns) with a guard test that every test project belongs to exactly one group.
- Release mode (`release: published`) behaviour and the OIDC publish path are unchanged. Packing
  and publishing stay in one job and must not run in parallel copies.
- Branch-protection required checks: if job names change, list the new check names in the PR so the
  maintainer can update protection. Never leave master unprotected.
- Target wall time for a PR: about the slowest group (expected ~5–6 min). Record before/after.

## Checklist
- [x] Test groups defined once in dev CLI (by convention) + guard test
- [x] Parallel jobs in workflow.yml; artifacts uploaded as before
- [x] Release/publish path unchanged and single-job
- [x] Before/after wall time; required-check names listed (after = projection until PR CI runs)
- [ ] Gates: `ganda repo audit` (passes); CI green on the PR itself (PR not open yet)

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
- 2026-10-15: implemented (headless implementer, ganda task work).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-15T08:50:20Z
- Review oracle: general reviewer (claude, claude-opus-5-5), effort 2, round 1 clean — 2026-10-15
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-15T08:52:06Z

## Results

### What changed
- `tools/dev-cli/services/test-groups.cs` (new) is the one definition of the test groups. Groups
  come from the project path by convention:
  - `fast`: everything outside `tests/container-apps/`, plus container-app `*-contracts-tests` and
    `*-domain-tests` (14 projects: analyzers, sourcegen, foundation, libraries, common, tools).
  - `playwright`: `*-playwright-tests` (1 project).
  - `integration`: the rest of `tests/container-apps/` (8 projects: `*-integration-tests`,
    aspire-tests, web-infrastructure-tests, `*-jaribu-tests` aggregators).
- `dev test --group <fast|playwright|integration>` runs one group. When `--group` is set, any
  project that matches zero groups or more than one stops the run before a test starts. This also
  protects generated apps.
- `dev workflow --phase test [--group X]` runs clean → build → that group (or all tests).
  `dev workflow --phase publish` runs clean → build → publish compose/kubernetes/aca. With no
  `--phase`, the pipeline runs everything in order, as before. Release mode rejects
  `--phase`/`--group`. Unknown phases or groups, and `--group` without `--phase test`, exit 1
  before any build starts.
- Guard tests in `tests/tools/dev-cli-tests/test-groups-tests.cs`:
  - every `tests/**/*.csproj` belongs to exactly one group;
  - every group has at least one monorepo project;
  - sample paths map to the expected group;
  - a misfiled playwright project matches two groups, so the guard catches it.
- `.github/workflows/workflow.yml` jobs:
  - `ci-group`: a matrix over fast, playwright, integration and publish, each running
    `dev workflow <args>`, with `fail-fast: false`. Playwright screenshots upload only from
    `ci (playwright)`. The compose, helm and aca uploads come only from `ci (publish)`. Artifact
    names and retention are unchanged. Only the publish job installs the Aspire CLI (no test
    shells out to `aspire`).
  - `ci`: a roll-up that fails if any `ci-group` job failed or was cancelled, and passes when
    they all passed or were skipped (no CI paths changed).
  - `release`: runs on release and workflow_dispatch, as one job. It does the OIDC login, the
    probe, one `dev workflow` pass in Release mode and the `Packages-*` upload, with the same
    steps and conditions as before. It is not a matrix. NuGet Trusted Publishing binds to the
    repo + `workflow.yml`, not the job name.
- `documentation/developer/guides/releasing.md`: describes the new job layout.

### Shared build artifact vs build-per-job (measured)
- Each job builds what it needs. After a solution Release build, local `bin/`+`obj/` is **2.2 GB**
  (**~800 MB** gzipped). The org Free plan caps shared Actions storage at **500 MB**, so a shared
  build artifact cannot be stored at all. Uploading and downloading it would also cost more than
  the ~82 s solution build each job repeats in parallel.

### Wall time
- Before, single `ci` job: ~14 min on the 2026-10-15 baseline; **10m12s** after 365 (run
  38705082403: 82 s build, 6m38s tests, ~1m25s publishes). Per-group test time in that run:
  - fast ≈ 40 s
  - playwright ≈ 2m20s
  - integration ≈ 3m50s (aspire-tests 65 s, web-spa-integration 49 s, web-infrastructure 37 s,
    api/web-server integration ~30 s each)
  - publish ≈ 1m25s
- After, projected: each job pays ~2m10s setup (checkout/SDK ~20 s, runfile compile ~24 s,
  build 82 s), then its group. That gives integration ≈ 6 min (the slowest), playwright ≈ 4.5 min,
  publish ≈ 3.5 min, fast ≈ 3 min. PR wall time is about **6 min**, down from ~10 min.
  template-smoke (~6 min) still runs in parallel. The real figure comes from the PR's own CI run
  (the workflow does not trigger on `task/**` pushes before the PR opens).
- Measured locally: `dev workflow --phase test --group fast` took 1m32s (incremental build,
  14/14 projects passed).

### Required checks
- master branch protection has **no required status checks** today (only PR-required), so nothing
  must change. New check names: `ci (fast)`, `ci (playwright)`, `ci (integration)`,
  `ci (publish)`, plus the roll-up `ci`, which keeps the old name. If protection should require
  CI, require `ci`: it covers all four groups. On release/dispatch the job is now `release`
  (previously `ci`).

### Review disposition
- 1 round, effort 2, roster: general (review oracle, claude-opus-5-5).
- Final counts: 0 bug, 0 suggestion, 0 nit; 0 open, 0 fixed, 0 wontfix.
- Disposition: **clean**. Re-verified: dev-cli-tests 235/235, `ganda repo audit` passes.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

### How to validate
- Smoke: `dotnet run tools/dev-cli/dev.cs -- workflow --phase test --group fast`
  Expect: `Pipeline: clean -> build -> test (fast)`, `Test group 'fast': 14 of 23 projects`, all
  pass, `Pipeline SUCCEEDED`, exit 0.
- Smoke: `dotnet run tools/dev-cli/dev.cs -- workflow --phase test --group nope; echo $?`
  Expect: `Error: Unknown test group 'nope'. Groups: fast, playwright, integration.` and exit 1,
  no build.
- Smoke: `dotnet run tools/dev-cli/dev.cs -- workflow --mode release --phase test; echo $?`
  Expect: `--phase and --group apply to pr/merge only; release runs as one job.` and exit 1.
- Smoke: `cd tests/tools/dev-cli-tests && dotnet test -c Release`
  Expect: 235 passed, including `GroupsOf_Given_.EveryTestProject_Should_BelongToExactlyOneGroup`.
- Smoke: the PR's CI run.
  Expect: `ci (fast)`, `ci (playwright)`, `ci (integration)` and `ci (publish)` run in parallel
  and pass. `ci` passes. The `playwright-N`, `aspire-compose-N`, `aspire-helm-N` and
  `aspire-aca-N` artifacts are uploaded. Wall time is ~6 min, versus 10m12s before.
- Gates: `ganda repo audit` passes; the dev CLI runfile builds with 0 warnings; the solution
  build has 0 warnings and 0 errors.

## Review

- Round 1: Done
