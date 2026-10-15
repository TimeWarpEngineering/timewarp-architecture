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
- [ ] `--no-build`/`--no-restore` path used by `dev workflow`; default `dev test` still builds
- [ ] Out-of-solution test projects built first (or added to .slnx)
- [ ] dev-cli tests for argument construction; CI timing before/after
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
