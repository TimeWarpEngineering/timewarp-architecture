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
- [ ] Test groups defined once in dev CLI (by convention) + guard test
- [ ] Parallel jobs in workflow.yml; artifacts uploaded as before
- [ ] Release/publish path unchanged and single-job
- [ ] Before/after wall time; required-check names listed
- [ ] Gates: `ganda repo audit`; CI green on the PR itself

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
