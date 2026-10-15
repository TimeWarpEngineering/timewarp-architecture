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
- [ ] Shallow checkout where safe; full history kept and justified where needed
- [ ] NuGet cache keyed on CPM hash in every restoring job
- [ ] Runfile cache decision recorded
- [ ] Gates: `ganda repo audit`; CI green on the PR

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
