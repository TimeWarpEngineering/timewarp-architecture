# dev test runs test projects with --no-build after the solution build

## Why
Baseline (2026-10-15, run starting 07:17): `ci` job ~14 min = runfile compile 43s + `dev build` 89s + `dev test` ~9m45s (25 projects, one at a time) + 3 publishes ~2m. `template-smoke` ~6.5 min runs in parallel. Part of the CI speed-up stack 365 → 366 → 367 → 368 → 369 → 370.

`tools/dev-cli/endpoints/test-command.cs` runs `dotnet test -c Release` per project without
`--no-build`, so each of the ~25 projects re-checks its build right after `dev build`. That costs
about 5–8s of fixed overhead each, roughly 2–2.5 min in CI.

## Requirements
- When the solution was just built in the same configuration (the `dev workflow` pipeline, or an
  explicit flag), pass `--no-build` (and `--no-restore`) to each `dotnet test`.
- Standalone `dev test` keeps building by default, so a developer never runs stale binaries.
  Add a `--no-build` option to `dev test` that `dev workflow` uses. Record the choice.
- Make sure every test project is actually built by the solution build. The three projects outside
  the `.slnx` (`timewarp-testing-tests`, `api-jaribu-tests`, `web-jaribu-tests`, found during
  342) must be built explicitly before `--no-build` runs, or added to the solution. Record which.
- Before/after timing of `dev test` in CI, in the PR.

## Checklist
- [x] `--no-build`/`--no-restore` path used by `dev workflow`; default `dev test` still builds
- [x] Out-of-solution test projects built first (or added to .slnx)
- [x] dev-cli tests for argument construction; CI timing before (after = this PR's CI run)
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
- 2026-10-15 implementer (ganda task work): option, workflow wiring, argument helper + tests, timings.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-15T08:18:45Z

## Results

### Decisions
- **Opt-in flag.** `dev test` builds every project by default (`dotnet test -c Release`), so a
  developer never runs stale binaries. `dev test --no-build` passes `--no-build --no-restore`.
  `dev workflow` (Pr/Merge) calls it with `NoBuild = true` right after the Release solution build.
- **Out-of-solution projects keep building; the .slnx is unchanged.** `api-jaribu-tests`,
  `web-jaribu-tests` and `timewarp-testing-tests` stay out of the .slnx on purpose: the solution
  build never compiles co-located runfiles (`tw-feature-placement`). `dev test --no-build` reads
  the .slnx `Project Path` list. A test project that is not listed there runs without
  `--no-build`, so its own `dotnet test` builds it first.
- Argument construction is a pure helper, `tools/dev-cli/services/dotnet-test-arguments.cs`
  (`DotNetTestArguments`). It is compiled into `dev-cli-tests` and covered there by
  `dotnet-test-arguments-tests.cs`.

### Timing
- **Before (CI, master push run 38699376705):** the `dev test` step took 7m15s (07:34:29 →
  07:41:44) and the whole `Run CI Pipeline` step took 10m33s. Small suites already finish in
  1.2–1.7s each in CI (for example web-contracts-tests 1.7s and foundation-contracts-tests 1.4s),
  so the per-project build re-check is about 1s or less on the runner, not 5–8s.
- **Local (warm build server):** `dev test --no-build` took 7m10s for all 23 projects, all
  passing. Per project, `dotnet test` takes about 4.7s with `--no-build --no-restore` and 3–5s with
  the default build. Locally the difference is within noise.
- **After (CI):** read the `Testing …` timestamps for the `dev test` step in this PR's `ci` job.
  Expect a saving of tens of seconds at most, not the 2–2.5 min this task estimated.

### How to validate
**Smoke:**
```bash
dotnet run tools/dev-cli/dev.cs -- build
dotnet run tools/dev-cli/dev.cs -- test --no-build
(cd tests/tools/dev-cli-tests && dotnet test -c Release)
dotnet run tools/dev-cli/dev.cs -- test --help
```
**Expect:**
- The build reports 0 warnings and 0 errors.
- `test --no-build` ends with "Tests completed successfully!". The out-of-solution
  aggregators (`api-jaribu-tests`, `web-jaribu-tests`, `timewarp-testing-tests`) still compile
  inside their own `dotnet test`. The solution projects do not rebuild.
- `dev-cli-tests` passes 231/231, including `DotNetTestArguments_.*`.
- `test --help` lists `--no-build`.
- In the CI log, the `ci` job's "Testing …" lines appear right after "Build completed
  successfully!", with no MSBuild output for the solution test projects.

### Review disposition
- Rounds: 1. Effort 1, roster: general (review oracle, claude-opus-5-5).
- Final counts: bug 0 / suggestion 0 / nit 0. All counts are open 0, fixed 0, wontfix 0.
- Disposition: **clean**. No findings were raised.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.
